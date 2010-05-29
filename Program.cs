using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.ServiceProcess;
using Microsoft.Win32;

namespace Program_Launcher
{
    class Program
    {
        public static bool Error = false;
        static void Main(string[] args)
        {
            try
            {
                var configpath = "Config.txt";
                if (!string.IsNullOrEmpty(args[0]) && File.Exists(args[0]))
                {
                    configpath = args[0];
                    
                }
                Console.WriteLine("Config file set to: {0}",configpath);
                if (File.Exists(configpath))
                {
                    #region set width&height and read config.txt
                    var config = new StreamReader(configpath);
                    #endregion
                    while (config.EndOfStream == false)
                    {
                        #region read next line and display
                        var currentLine = config.ReadLine();
                        Console.WriteLine("Processing line:" + currentLine);
                        #endregion
                        #region execute file
                        if (currentLine.StartsWith("file:"))
                        {
                            try
                            {
                                currentLine = currentLine.Remove(0, 5);
                                var process = new Process { StartInfo = { FileName = currentLine, CreateNoWindow = false, UseShellExecute = true} };
                                currentLine = config.ReadLine();
                                if (currentLine.StartsWith("workingdir:"))
                                {
                                    currentLine = currentLine.Remove(0, 11);
                                    process.StartInfo.WorkingDirectory = currentLine;
                                }
                                else
                                {
                                    Console.WriteLine("You need to add the working dir for the process above the {0} line in the config! - attempting to start program anyway", currentLine);
                                }
                                process.Start();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message + "\n\n" + e.StackTrace);
                                Error = true;
                            }
                        }
                        #endregion
                        #region wait

                        try
                        {
                            if (currentLine.StartsWith("wait:"))
                            {
                                currentLine = currentLine.Remove(0, 5);
                                Console.WriteLine("Sleeping for {0} milliseconds", currentLine);
                                Thread.Sleep(Convert.ToInt32(currentLine));
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error occured while attempting to sleep for {0} milliseconds",currentLine);
                            Console.WriteLine("Tech Error: {0}",e.Message);
                        }

                        #endregion
                        #region #
                        if (currentLine.StartsWith("#"))
                        {
                            continue;
                        }
                        #endregion
                        #region terminate program
                        if (currentLine.StartsWith("terminate:"))
                        {
                            try
                            {
                                currentLine = currentLine.Remove(0, 10);
                                var terminateProcesses = Process.GetProcessesByName(currentLine);
                                foreach (var processes in terminateProcesses)
                                {
                                    processes.Kill();
                                    processes.WaitForExit((int)TimeSpan.FromSeconds(30).TotalMilliseconds);
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message + "\n\n" + e.StackTrace);
                                Error = true;
                            }
                        }
                        #endregion
                        #region Safe Close
                        try
                        {
                            if (currentLine.ToLower().StartsWith("safeclose:"))
                            {
                                currentLine = currentLine.Remove(0, 10);
                                var closeProcess = Process.GetProcessesByName(currentLine);
                                foreach (var process in closeProcess)
                                {
                                    process.CloseMainWindow();
                                    process.WaitForExit((int) TimeSpan.FromSeconds(60).TotalMilliseconds);
                                }
                            }
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine("Error occured while trying to safely close {0}",currentLine);
                            Console.WriteLine("Tech Error: {0}",e.Message);
                        }

                        #endregion
                        #region copy file
                        if (currentLine.StartsWith("copyfrom:"))
                        {
                            try
                            {
                                currentLine = currentLine.Remove(0, 9);
                                string nextLine = config.ReadLine();
                                if (nextLine.StartsWith("#")) { nextLine = config.ReadLine(); }
                                if (nextLine.StartsWith("copyto:"))
                                {
                                    nextLine = nextLine.Remove(0, 7);
                                    File.Copy(currentLine, nextLine, true);
                                }
                                else
                                {
                                    Console.WriteLine("Ignored line:{0} because of missing copyto: ", currentLine);
                                }
                                continue;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message + "\n\n" + e.StackTrace);
                                Error = true;
                            }
                        }
                        #endregion
                        #region stop service
                        if (currentLine.StartsWith("stopservice:"))
                        {
                            var servicename = currentLine.Remove(0, 12);
                            try
                            {
                                var service = new ServiceController(servicename);
                                if (service.Status != ServiceControllerStatus.Stopped)
                                {
                                    Console.WriteLine("Attempting to stop the {0} service", servicename);
                                    service.Stop();
                                    service.WaitForStatus(ServiceControllerStatus.Stopped);
                                    Console.WriteLine("Service {0} is now stopped", servicename);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(
                                    "Failed to Stop the {0} process, debug info follows: \n", servicename);
                                Console.WriteLine(ex.InnerException + "\n" + ex.Message + ex.Source + ex.StackTrace);
                                Error = true;
                            }
                        }
                        #endregion
                        #region start service
                        if (currentLine.StartsWith("startservice:"))
                        {
                            var servicename = currentLine.Remove(0, 13);
                            try
                            {
                                var service = new ServiceController(servicename);
                                if (service.Status != ServiceControllerStatus.Running)
                                {
                                    Console.WriteLine("Attepting to start the {0} service", servicename);
                                    service.Start();
                                    service.WaitForStatus(ServiceControllerStatus.Running);
                                    Console.WriteLine("Service {0} is now running", servicename);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(
                                    "Failed to Start the {0} process, debug info follows: \n", servicename);
                                Console.WriteLine(ex.InnerException + "\n" + ex.Message + ex.Source + ex.StackTrace);
                                Error = true;
                            }
                        }
#endregion
                        #region Copy Directory
                        try
                        {
                            if (currentLine.ToLower().StartsWith("copydirfrom:"))
                            {
                                currentLine = currentLine.Remove(0, 12);
                                var copyfrom = currentLine;
                                currentLine = config.ReadLine();
                                if (currentLine.ToLower().StartsWith("copydirto:"))
                                {
                                    var copyto = currentLine.Remove(0, 10);
                                    if (Directory.Exists(copyfrom) && Directory.Exists(copyto))
                                    {
                                        var proc = new Process
                                                       {
                                                           StartInfo =
                                                               {
                                                                   UseShellExecute = false,
                                                                   FileName = @"C:\WINDOWS\system32\xcopy.exe",
                                                                   Arguments = string.Format(@"{0} {1} /E /I /H /Y", copyfrom, copyto),
                                                                   RedirectStandardOutput = true,
                                                                   CreateNoWindow = true
                                                               }
                                                       };
                                        proc.OutputDataReceived += proc_OutputDataReceived;
                                        proc.Start();
                                        proc.BeginOutputReadLine();
                                        proc.WaitForExit();
                                    }
                                }
                                else
                                {
                                    Console.WriteLine(
                                        "Next line was not copy from, please correct this! Attempting to continue");
                                }
                            }
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine("Failed to copy directory at line {0}",currentLine);
                            Console.WriteLine("Tech Error: {0}",e.Message);
                        }

                        #endregion
                        #region waitforprogram

                        try
                        {
                            if (currentLine.StartsWith("waitforprogram:"))
                            {
                                currentLine = currentLine.Remove(0, 15);
                                Console.WriteLine("Waiting for process {0} to close", currentLine);
                                var process = Process.GetProcessesByName(currentLine);
                                foreach (var p in process)
                                {
                                    p.WaitForExit((int) TimeSpan.FromSeconds(30).TotalMilliseconds);
                                }
                                Console.WriteLine("Process {0} has closed", currentLine);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error while trying to wait for {0} to close",currentLine);
                            Console.WriteLine("Tech Error: {0}", e.Message);
                        }
                        #endregion
                    }
                }
                else
                {
                    #region deal with creating config and closing
                    try
                    {
                        Console.WriteLine("Config.txt did not exist! Please now go and fill out the file with the various info that you need.");
                        var config = new StreamWriter("Config.txt");
                        config.WriteLine("# Use # at the beginning of a line if you want to add comments to this file\n# If you are entering a filepath which is to be exectued then put it as file:fullfilepathhere\n# Use a new line for each file, do not put more than 1 file on the same line or it will not work.\n# You can also use terminate:processname to kill a process\n# The operations will be processed from the top of the file to teh bottom.\n#Close the meh program so the file can be replaced\nterminate:meh\n#Start the meh program up, and also put its working directory underneath.\nfile:c:\\meh.exe\nworkingdir:c:\\\n#stop the service dwm (remember to use shortname!)\nstopservice:uxsms\n#start the dwm service\nstartservice:uxsms\n#copy the meh.exe file from c:\\\ncopyfrom:c:\\meh.exe\n#copy it to the lol folder\ncopyto:c:\\lol\\meh.exe");
                        config.Close();
                        Console.WriteLine("Example Config.txt created, please edit as needed!, program will close in 10seconds!");
                        Thread.Sleep(10000);
                        Environment.Exit(0);
                    }
                    catch
                    {
                        Console.WriteLine("An error occured while attempting to write the example config file, please check the program has access to its directory!");
                        Thread.Sleep(5000);
                        Error = true;
                    }
                    #endregion
                }
            }
            finally
            {
                if (Error)
                {
                    Console.WriteLine("Errors occured Exiting in 5 seconds..");
                    Thread.Sleep(5000);
                    Environment.Exit(1);
                }
            }
        }

        static void proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }
    }
}