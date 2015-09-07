using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace HTMLValid
{
    class Program
    {
        /// <summary>
        ///     Write to the console and set the exit code
        /// </summary>
        /// <param name="errorCode"></param>
        private static void Exit(ExitCode errorCode)
        {
            Console.WriteLine();
            Console.Write("Press any key to continue . . .");
            Console.ReadKey(true);
            Environment.Exit((sbyte) errorCode);
        }

        /// <summary>
        ///     Parse commandline arguments
        /// </summary>
        /// <param name="commandlineArgs">Array of commandline arguments</param>
        /// <param name="filePath">Filepath variable reference</param>
        /// <param name="isErrorsWarningOnly">Display warnings only variable reference</param>
        /// <returns>True a filepath was found; otherwise, false</returns>
        private static bool GetCommandLineArgs(string[] commandlineArgs, ref string filePath, ref bool isErrorsWarningOnly)
        {
            bool @return = false;
            foreach (string commandlineArg in commandlineArgs)
            {
                if (File.Exists(commandlineArg))
                {
                    filePath = commandlineArg;
                    // A search path was found
                    @return = true;
                    continue;
                }

                if (commandlineArg.ToLower() == "-allfiles")
                {
                    // Opposite of what was passed
                    isErrorsWarningOnly = !isErrorsWarningOnly;
                }
            }

            return @return;
        }

        /// <summary>
        ///     Check if a filepath is a directory
        /// </summary>
        /// <param name="filePath">Filepath to check</param>
        /// <returns>True is a directory; otherwise, false</returns>
        private static bool IsDir(string filePath)
        {
            return Directory.Exists(filePath) && !File.Exists(filePath);
        }

        /// <summary>
        ///     Check if a filepath is a file
        /// </summary>
        /// <param name="filePath">Filepath to check</param>
        /// <returns>True is a file; otherwise, false</returns>
        private static bool IsFile(string filePath)
        {
            return File.Exists(filePath) && !Directory.Exists(filePath);
        }

        /// <summary>
        ///     Main entry point
        /// </summary>
        /// <param name="commandlineArgs">Commandline arguments passed to the executable</param>
        private static void Main(string[] commandlineArgs)
        {
            // Set the enironment variables for the application
            Environment.SetEnvironmentVariable("PROGRAMNAME", "HTMLValid");
            Environment.SetEnvironmentVariable("FILEVERSION", "0.0.0.6");

            Console.Write(Environment.ExpandEnvironmentVariables(
                "=========================================================\n" +
                "|\t\t\t\t\t\t\t|\n" +
                "|\t%PROGRAMNAME%\t\t\t\t\t|\n" +
                "|\t\t\t\t\t\t\t|\n" +
                "|\tAuthor: SoftwareSpot (C) 2014-2015\t\t\t|\n" +
                "|\tBuild: 0.0.0.6\t\t\t\t\t|\n" +
                "|\t\t\t\t\t\t\t|\n" +
                "|\tUsage:\t\t\t\t\t\t|\n" +
                "|\t%PROGRAMNAME%.exe \"HTMLFile/Folder\" < -allfiles >\t|\n" +
                "|\t\t\t\t\t\t\t|\n" +
                "|\t<optional> -allfiles: Display all files.\t|\n" +
                "|\t\t\t\t\t\t\t|\n" +
                "=========================================================\n"));

            /* Exit codes:
             * 0 - Valid HTML/CSS file
             * 1 - Invalid HTML/CSS file
             *
             * -1 - No file/directory was passed or the directory was empty
             * -2 - File/directory invalid
             * -3 - Unable to connect to the W3C Validator
             * -4 - User cancelled the operation
             */

            const byte filecheckThreshold = 5;

            // Display only files with errors or warnings
            bool isErrorsWarningOnly = true;
            string searchPath = String.Empty;

            // If a filepath was not found in the commandline arguments, the return with a empty path exit code
            if (!GetCommandLineArgs(commandlineArgs, ref searchPath, ref isErrorsWarningOnly))
            {
                Console.WriteLine(Environment.ExpandEnvironmentVariables("Please pass a valid HTML/CSS file or directory to %PROGRAMNAME%."));
                Exit(ExitCode.EmptyPath);
            }

            bool isDir = IsDir(searchPath);
            if (!isDir && !IsFile(searchPath))
            {
                Console.WriteLine(Environment.ExpandEnvironmentVariables("Please pass a valid HTML/CSS file or directory to %PROGRAMNAME%."));
                Exit(ExitCode.InvalidPath);
            }

            // Create a new directory array
            string[] fileList;
            if (isDir)
            {
                fileList = Directory.GetFiles(searchPath, "*.*", SearchOption.AllDirectories);
                // If no files were found then exit with the empty path exit code
                if (fileList.Length == 0)
                {
                    Exit(ExitCode.EmptyPath);
                }
            }
            else // Otherwise it's a file as a check was done before to check if the file path was either a directory or file
            {
                // Workaround for when a single HTML/CSS file is passed to HTMLValid
                string[] tempFileList = { searchPath };

                // Set the directory array to the temporary array
                fileList = tempFileList;
            }

            if (fileList.Length > filecheckThreshold)
            {
                Console.WriteLine("The directory contains more than {0} files and could take anywhere between " +
                    "{1} second{2} to complete.{3}" +
                    "{3}" +
                    "This is a FREE service and thus W3C recommends a one second waiting period between uploads.{3}" +
                    "{3}" +
                    "Would you like to continue processing? (Y or N)", filecheckThreshold, fileList.Length, (fileList.Length > 1 ? "s" : ""), Environment.NewLine);

                string userChoice = String.Empty;
                byte failCount = 0;

                // Continue to loop until y or n is entered
                while (userChoice != "y" && userChoice != "n")
                {
                    // If the fail count is greater than zero then display a warning as to what is to be entered
                    if (failCount > 0)
                    {
                        Console.WriteLine();
                        Console.Write("Please enter either Y or N: ");
                    }
                    // Get the char and convert to string and lowercase
                    userChoice = Console.ReadKey(true).KeyChar.ToString(CultureInfo.InvariantCulture).ToLower();
                    failCount++;
                }
                if (userChoice == "n")
                {
                    Exit(ExitCode.UserClose);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Validating . . .");
            Console.WriteLine();

            ExitCode exitCode = ExitCode.W3CValid;

            // Set the file and valid count to zero
            int fileCount = 0;
            int validCount = 0;

            // Create a HTMLValid object and set the user agent string
            HtmlValid htmlValid = new HtmlValid(Environment.ExpandEnvironmentVariables("%PROGRAMNAME%"));

            // Create a timer object
            Stopwatch totalTimer = Stopwatch.StartNew();

            // As the directory array isn't being written to then a foreach() is preferred
            foreach (string filePath in fileList)
            {
                // Returns a HTMLValidResults structure
                HtmlValidResults htmlResults = htmlValid.ValidateFilePath(filePath, true);

                // If the file isn't a HTML or CSS file then skip the file
                if (htmlResults.FileType == HtmlValidFileType.NonSupportedFile)
                {
                    continue;
                }

                // An error occurred with connecting to W3C so break from the loop
                if (!htmlResults.IsConnected)
                {
                    Console.WriteLine("An error occurred connecting to W3C's validation service.{0}Please try again or contact your local administrator.",
                        Environment.NewLine);
                    exitCode = ExitCode.W3CConnection;
                    break;
                }

                validCount += (htmlResults.Status == HtmlValidStatus.Valid ? 1 : 0);
                fileCount++;

                // If only display errors and warnings and no errors or warnings found then continue
                if (isErrorsWarningOnly && htmlResults.Errors == 0 && htmlResults.Warnings == 0)
                {
                    continue;
                }

                string fileName = PathCompactPathEx(filePath, 15);
                Console.WriteLine("The {0} file {1} was {2}.",
                    (htmlResults.FileType == HtmlValidFileType.Css ? "CSS" : "HTML"), fileName, (htmlResults.Status == HtmlValidStatus.Valid ? "Valid" : "Invalid"));

                // Print out the errors and/or warnings
                if (htmlResults.Errors > 0 || htmlResults.Warnings > 0)
                {
                    Console.WriteLine("! It appears there are additional issues that should be addressed: ");
                    if (htmlResults.Errors > 0)
                    {
                        Console.WriteLine("Errors: {0}", htmlResults.Errors);
                    }
                    if (htmlResults.Warnings > 0)
                    {
                        Console.WriteLine("Warnings: {0}", htmlResults.Warnings);
                    }
                }
                Console.WriteLine();

                if (fileList.Length >= 2)
                {
                    // Recommendation by W3C is to sleep for 1 second, though this will only happen if there are 2 or more files
                    Thread.Sleep(1000);
                }
            }

            // If equal to zero then no major error occurred during the loop
            if (exitCode == ExitCode.W3CValid)
            {
                // If all files were valid then set the exit code to a valid filepath
                exitCode = (validCount == fileList.Length ? ExitCode.W3CValid : ExitCode.W3CInvalid);

                // Get the total number of elapsed seconds and cast as an integer
                int seconds = (int) totalTimer.ElapsedMilliseconds / 1000;
                Console.WriteLine("Created: {0}{5}Files: {1}{5}Valid: {2}{5}Running: {3} second{4}.",
                    DateTime.Now, fileCount, validCount, seconds, (seconds == 1 ? "" : "s"), Environment.NewLine);
            }

            Exit(exitCode);
        }

        /// <summary>
        ///     Compact a filepath by replacing with ellipses
        /// </summary>
        /// <param name="filePath">Filepath to compact</param>
        /// <param name="length">Total length of the filepath</param>
        /// <returns>Compacted filepath string; otherwise, original string</returns>
        private static string PathCompactPathEx(string filePath, int length = 25)
        {
            if (!String.IsNullOrEmpty(filePath) && (filePath.Length - Path.GetFileName(filePath).Length) > length)
            {
                // Shorten the path but still retaining the file name length
                filePath = Regex.Replace(filePath, @"^(.{" + length + @"}).+?([^\\]+)$", @"$1...\$2");
            }

            return filePath;
        }
    }
}
