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
        private static void Exit(ExitCode errorCode)
        {
            Console.WriteLine();
            Console.Write("Press any key to continue . . .");
            Console.ReadKey(true);
            Environment.Exit((sbyte) errorCode);
        }

        private static bool GetCommandLineArgs(string[] commandlineArgs, ref string filePath, ref bool isErrorsWarningOnly)
        {
            bool @return = false;
            foreach (string commandline in commandlineArgs)
            {
                if (File.Exists(commandline))
                {
                    filePath = commandline;
                    @return = true; // A search path was found
                    continue;
                }

                if (commandline.ToLower() == "-allfiles")
                {
                    isErrorsWarningOnly = !isErrorsWarningOnly; // Opposite of what was passed
                }
            }

            return @return;
        }

        private static bool IsDir(string filePath)
        {
            return Directory.Exists(filePath) && !File.Exists(filePath);
        }

        private static bool IsFile(string filePath)
        {
            return File.Exists(filePath) && !Directory.Exists(filePath);
        }

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

            bool isErrorsWarningOnly = true; // Display only files with errors or warnings
            string searchPath = String.Empty;
            if (GetCommandLineArgs(commandlineArgs, ref searchPath, ref isErrorsWarningOnly)) // Set the filePath as the one sent to the application as a commandline parameter
            {
                searchPath = commandlineArgs[0];
            }
            else
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

            string[] fileList; // Create a new directory array
            if (isDir)
            {
                fileList = Directory.GetFiles(searchPath, "*.*", SearchOption.AllDirectories);
                if (fileList.Length == 0) // If no files were found then exit with the exit code of EXIT_EMPTY_PATH
                {
                    Exit(ExitCode.EmptyPath);
                }
            }
            else // Otherwise it's a file as a check was done before to check if the file path was either a directory or file
            {
                string[] tempFileList = { searchPath }; // Workaround for when a single HTML/CSS file is passed to HTMLValid
                fileList = tempFileList; // Set the directory array to the temporary array
            }

            if (fileList.Length > filecheckThreshold)
            {
                Console.WriteLine("The directory contains more than {0} files and could take anywhere between " +
                    "{1} second{2} to complete.\n" +
                    "\n" +
                    "This is a FREE service and thus W3C recommends a one second waiting period between uploads.\n" +
                    "\n" +
                    "Would you like to continue processing? (Y or N)", filecheckThreshold, fileList.Length, (fileList.Length > 1 ? "s" : ""));

                string userChoice = String.Empty;
                byte failCount = 0;
                while (userChoice != "y" && userChoice != "n") // Continue to loop until y or n is entered
                {
                    if (failCount > 0) // If the fail count is greater than zero then display a warning as to what is to be entered
                    {
                        Console.WriteLine();
                        Console.Write("Please enter either Y or N: ");
                    }
                    userChoice = Console.ReadKey(true).KeyChar.ToString(CultureInfo.InvariantCulture).ToLower(); // Get the char and convert to string and lowercase
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
            int fileCount = 0, validCount = 0; // Set the file and valid count to zero
            HtmlValid htmlValid = new HtmlValid(Environment.ExpandEnvironmentVariables("%PROGRAMNAME%")); // Create a HTMLValid object and set the UserAgent

            Stopwatch totalTimer = Stopwatch.StartNew(); // Create a timer object
            foreach (string filePath in fileList) // As the directory array isn't being written to then a foreach() is preferred
            {
                HtmlValidResults htmlResults = htmlValid.ValidateFilePath(filePath, true); // Returns a HTMLValidResults object
                if (htmlResults.FileType == HtmlValidFileType.NonSupportedFile) // If the file isn't a HTML or CSS file then skip the file
                {
                    continue;
                }

                if (!htmlResults.IsConnected) // An error occurred with connecting to W3C so break from the loop
                {
                    Console.WriteLine("An error occurred connecting to W3C's validation service.\n" +
                        "Please try again or contact your local administrator.");
                    exitCode = ExitCode.W3CConnection;
                    break;
                }

                validCount += (htmlResults.Status == HtmlValidStatus.Valid ? 1 : 0);
                fileCount++;
                if (isErrorsWarningOnly && htmlResults.Errors == 0 && htmlResults.Warnings == 0) // If only display errors and warnings and no errors or warnings found then continue
                {
                    continue;
                }

                string fileName = PathCompactPathEx(filePath, 15);
                Console.WriteLine("The {0} file {1} was {2}.", (htmlResults.FileType == HtmlValidFileType.Css ? "CSS" : "HTML"), fileName, (htmlResults.Status == HtmlValidStatus.Valid ? "Valid" : "Invalid"));

                if (htmlResults.Errors > 0 || htmlResults.Warnings > 0) // Print out the errors and/or warnings
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
                    Thread.Sleep(1000); // Recommendation by W3C is to sleep for 1 second, though this will only happen if there are 2 or more files
                }
            }

            if (exitCode == ExitCode.W3CValid) // If equal to zero then no major error occurred during the loop
            {
                exitCode = (validCount == fileList.Length ? ExitCode.W3CValid : ExitCode.W3CInvalid); // If all files were valid then set the exit code to EXIT_VALID_HTML
                int seconds = (int) totalTimer.ElapsedMilliseconds / 1000; // Get the total number of elapsed seconds and cast as an integer
                Console.WriteLine("Created: {0}\n" +
                    "Files: {1}\n" +
                    "Valid: {2}\n" +
                    "Running: {3} second{4}.", 
                    DateTime.Now, fileCount, validCount, seconds, (seconds == 1 ? "" : "s"));
            }
            Exit(exitCode);
        }

        private static string PathCompactPathEx(string filePath, int length = 25)
        {
            if (!String.IsNullOrEmpty(filePath) && (filePath.Length - Path.GetFileName(filePath).Length) > length)
            {
                filePath = Regex.Replace(filePath, @"^(.{" + length + @"}).+?([^\\]+)$", @"$1...\$2"); // Shorten the path but still retaining the file name length
            }
            return filePath;
        }
    }
}