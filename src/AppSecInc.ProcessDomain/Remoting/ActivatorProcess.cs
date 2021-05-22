using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using log4net;
using AppSecInc.ProcessDomain.Utils;

namespace AppSecInc.ProcessDomain.Remoting
{
    public delegate void AttachedDelegate();
    public delegate void DetachedDelegate();

    /// <summary>
    /// Represents a process for a Process Domain and handles things such as attach/detach events and restarting the process
    /// </summary>
    internal class ActivatorProcess : IDisposable
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(ActivatorProcess));
        public const string AssemblyName = "{0}.exe";
        public const string ConfigName = "{0}.ctl";

        public event AttachedDelegate Attached;
        public event DetachedDelegate Detached;

        readonly ProcessDomainSetup _setupInfo;
        readonly string _assemblyFile;
        readonly Process _process;
        readonly string _friendlyName;
        readonly string _setupInfoFile;
        ProcessStatus _processStatus;
        ActivatorClient _client;
        bool _disposed;

        private delegate void DeleteAssemblyFileDelegate(ManualResetEvent cancelEvent);

        public ActivatorProcess(string friendlyName, ProcessDomainSetup setupInfo)
        {
            Logger.InfoFormat("Creating ActivatorProcess for Process Domain '{0}' with the following configuration:", friendlyName);
            LogProcessDomainSetup(setupInfo);

            _friendlyName = friendlyName;
            _setupInfo = setupInfo;
            _assemblyFile = ActivatorHostAssemblyGenerator.CreateRemoteHostAssembly(friendlyName, setupInfo);
            Logger.InfoFormat("Generated Assembly: {0}", _assemblyFile);

            var startInfo = new ProcessStartInfo
            {
                FileName = _assemblyFile,
                CreateNoWindow = true,
                UseShellExecute = false,
                ErrorDialog = false,
                WorkingDirectory = _setupInfo.WorkingDirectory,
            };

            if (_setupInfo.EnvironmentVariables != null)
            {
                foreach (var kv in _setupInfo.EnvironmentVariables)
                {
                    startInfo.EnvironmentVariables[kv.Key] = kv.Value;
                }
            }

            _process = new Process
            {
                StartInfo = startInfo
            };

            _setupInfoFile = Path.Combine(setupInfo.ExecutableDirectory, string.Format(ConfigName, friendlyName));

            _process.Exited += Process_Exited;
            _process.EnableRaisingEvents = true;
        }

        private static void LogProcessDomainSetup(ProcessDomainSetup setupInfo)
        {
            Logger.InfoFormat("ExecutableDirectory = {0}", setupInfo.ExecutableDirectory);
            Logger.InfoFormat("ProcessStartTimeout = {0}", setupInfo.ProcessStartTimeout);
            Logger.InfoFormat("FileDeletionTimeout = {0}", setupInfo.FileDeletionTimeout);
            Logger.InfoFormat("DeleteOnUnload = {0}", setupInfo.DeleteOnUnload);
            Logger.InfoFormat("RestartOnProcessExit = {0}", setupInfo.RestartOnProcessExit);
            Logger.InfoFormat("WorkingDirectory = {0}", setupInfo.WorkingDirectory);
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            LogExitReason();

            var tmp = Detached;

            if (tmp != null)
            {
                tmp();
            }

            // If Process Status is Active is because it was running and ended unexpectedly, 
            // If status is Terminated then it was due to Terminate method invoked.
            if (_setupInfo.RestartOnProcessExit 
                && _processStatus != ProcessStatus.Killed
                && _client != null)
            {
                Logger.InfoFormat("Restarting process for Process Domain '{0}'", _friendlyName);
                try
                {
                    Start();
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Error restarting process for Process Domain '{0}'", _friendlyName), ex);
                }
            }
        }

        /// <summary>
        /// Logs the current action taken.
        /// </summary>
        private void LogExitReason()
        {
            switch (_processStatus)
            {
                case ProcessStatus.Active:
                    Logger.WarnFormat("Process Domain '{0}' process exited unexpectedly", _friendlyName);
                    break;
                case ProcessStatus.Killed:
                    Logger.InfoFormat("Process Domain '{0}' process exited", _friendlyName);
                    break;
                case ProcessStatus.Terminated:
                    Logger.InfoFormat("Process Domain '{0}' process restarted", _friendlyName);
                    break;
            }
        }

        /// <summary>
        /// Starts the remote process which will host an Activator
        /// </summary>
        public void Start()
        {
            CheckDisposed();
            DisposeClient();

            Logger.InfoFormat("Starting process for Process Domain '{0}'", _friendlyName);

            String processGuid = Guid.NewGuid().ToString();

            bool created;
            var serverStartedHandle = new EventWaitHandle(false, EventResetMode.ManualReset, string.Format(ActivatorHost.EventName, processGuid), out created);

            // We set guid to a new value every time therefore this "should" never happen.
            if (!created)
            {
                throw new Exception("Event handle already existed for remote process");
            }

            string processDomainAssemblyPath = AssemblyUtils.GetFilePathFromFileUri(typeof(ActivatorProcess).Assembly.CodeBase);
            ProcessDomainSetup.Serialize(_setupInfo, _setupInfoFile);

            // args[0] = process domain assembly path
            // args[1] = guid
            // args[2] = process id
            // args[3] = ProcessDomainSetup file
            _process.StartInfo.Arguments = string.Format("\"{0}\" {1} {2} \"{3}\"", processDomainAssemblyPath, processGuid, Process.GetCurrentProcess().Id, _setupInfoFile);
            
            if (!_process.Start())
            {
                throw new Exception(string.Format("Failed to start process from: {0}", _process.StartInfo.FileName));
            }

            Logger.InfoFormat("Process successfully started with process id {0}", _process.Id);

            if (!serverStartedHandle.WaitOne(_setupInfo.ProcessStartTimeout))
            {
                throw new Exception("Timed-out waiting for remote process to start");
            }

            serverStartedHandle.Close();
            
            _processStatus = ProcessStatus.Active;
            _process.PriorityClass = _setupInfo.PriorityClass;
            _client = new ActivatorClient(processGuid, _setupInfo);
            
            var tmp = Attached;
            if (tmp != null)
            {
                tmp();
            }
        }

        /// <summary>
        /// A proxy to the remote activator to use to create remote object instances
        /// </summary>
        public Activator Activator
        {
            get { return _client != null ? _client.Activator : null; }
        }

        /// <summary>
        /// Terminates this process, then it starts again.
        /// </summary>
        public void Terminate()
        {
            Logger.InfoFormat("Ternating Process Domain '{0}' process", _friendlyName);
            _processStatus = ProcessStatus.Terminated;

            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to terminate Process Domain '{0}' process due to an exception", ex);
                throw;
            }
        }

        /// <summary>
        /// Kills the remote process.
        /// </summary>
        public void Kill()
        {
            Logger.InfoFormat("Killing Process Domain '{0}' process", _friendlyName);
            _processStatus = ProcessStatus.Killed;

            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill();
                    _process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to kill Process Domain '{0}' process due to an exception", ex);
                throw;
            }

            FreeResources();
        }

        /// <summary>
        /// It deletes the assembly and the setup info file from the system.
        /// </summary>
        private void FreeResources()
        {
            if (_setupInfo.DeleteOnUnload)
            {
                DeleteAssemblyFileDelegate deleteAssemblyFileDelegate = DeleteAssemblyFile;

                var cancelEvent = new ManualResetEvent(false);
                var result = deleteAssemblyFileDelegate.BeginInvoke(cancelEvent, null, null);

                Logger.InfoFormat("Deleting file with timeout: {0}", _setupInfo.FileDeletionTimeout);
                if (!result.AsyncWaitHandle.WaitOne(_setupInfo.FileDeletionTimeout))
                {
                    //this will send a signal to cancel delete operation.
                    cancelEvent.Set();
                }

                // Free resourses and get the last exception thrown by delete file logic.
                // we need a loop here because operating system might not necessarily release file handle immediately
                // after stopping the process.
                try
                {
                    deleteAssemblyFileDelegate.EndInvoke(result);
                }
                catch (Exception lastException)
                {
                    Logger.Error(string.Format("Failed to delete Process Domain '{0}' assembly due to an exception", _friendlyName), lastException);
                    throw new DeleteOnUnloadException(string.Format("Failed to delete Process Domain '{0}' assembly", _friendlyName), lastException);
                }

                try
                {
                    File.Delete(_setupInfoFile);
                }
                catch (Exception lastException)
                {
                    Logger.Error(string.Format("Failed to delete Process Domain '{0}' configuration file due to an exception", _friendlyName), lastException);
                    throw new DeleteOnUnloadException(string.Format("Failed to delete Process Domain '{0}' configuration file", _friendlyName), lastException);
                }
            }
        }

        /// <summary>
        /// Deletes assembly file from the disk.
        /// This method will try until it succeeds or until stopped by an even.
        /// </summary>
        private void DeleteAssemblyFile(ManualResetEvent cancelEvent)
        {
            bool deleted = false;
            bool canceled;

            Exception lastException = null;

            do
            {
                try
                {
                    File.Delete(_assemblyFile);
                    if (_setupInfo.CopyConfigurationFile)
                    {
                        var configFile = _assemblyFile + ".config";
                        if (File.Exists(configFile))
                            File.Delete(configFile);
                    }
                    deleted = true;
                }
                catch (Exception ex)
                {
                    //save last exception.
                    lastException = ex;
                    Thread.Sleep(100);
                }

                canceled = cancelEvent.WaitOne(0);

            } while (!deleted && !canceled);

            if (!deleted && lastException != null)
            {
                throw lastException;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            DisposeClient();
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("ActivatorProcess");
            }
        }

        private void DisposeClient()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }
    }
}