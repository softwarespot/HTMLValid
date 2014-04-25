using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace HTMLValid
{
    internal class Program
    {
        private static void Exit(sbyte errorCode)
        {
            Console.WriteLine(); // Empty line.
            Console.Write("Press any key to continue . . .");
            Console.ReadKey(true);
            Environment.Exit(errorCode);
        }

        private static bool GetCommandLineArgs(string[] commandlineArgs, ref string filePath, ref bool isErrorsWarningOnly)
        {
            bool ret = false;
            foreach (string commandline in commandlineArgs)
            {
                if (File.Exists(commandline))
                {
                    filePath = commandline;
                    ret = true; // A search path was found.
                    continue;
                }

                if (commandline.ToLower() == "-allfiles")
                {
                    isErrorsWarningOnly = !isErrorsWarningOnly; // Opposite of what was passed.
                    continue;
                }
            }
            return ret;
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
            // Set the enironment variables for the application.
            Environment.SetEnvironmentVariable("PROGRAMNAME", "HTMLValid");
            Environment.SetEnvironmentVariable("FILEVERSION", "0.0.0.5");

            Console.Write(Environment.ExpandEnvironmentVariables(
                "=========================================================\n" +
                "|\t\t\t\t\t\t\t|\n" +
                "|\t%PROGRAMNAME%\t\t\t\t\t|\n" +
                "|\t\t\t\t\t\t\t|\n" +
                "|\tAuthor: SoftwareSpot (C) 2014\t\t\t|\n" +
                "|\tBuild: 0.0.0.6\t\t\t\t\t|\n" +
                "|\t\t\t\t\t\t\t|\n" +
                "|\tUsage:\t\t\t\t\t\t|\n" +
                "|\t%PROGRAMNAME%.exe \"HTMLFile/Folder\" < -allfiles >\t|\n" +
                "|\t\t\t\t\t\t\t|\n" +
                "|\t<optional> -allfiles: Display all files.\t|\n" +
                "|\t\t\t\t\t\t\t|\n" +
                "=========================================================\n"));

            /* Error codes:
             * 0 - Valid HTML/CSS file.
             * 1 - Invalid HTML/CSS file.
             *
             * -1 - No file/directory was passed or the directory was empty.
             * -2 - File/directory invalid.
             * -3 - Unable to connect to the W3C Validator.
             * -4 - User cancelled the operation.
             */

            // Constants for the exit message.
            const sbyte EXIT_USER_CLOSE = -4, EXIT_W3C_CONNECTION = -3, EXIT_INVALID_PATH = -2, EXIT_EMPTY_PATH = -1, EXIT_W3C_INVALID = 0, EXIT_W3C_VALID = 1;
            const byte FILECHECK_THRESHOLD = 5;

            bool isErrorsWarningOnly = true; // Display only files with errors or warnings.
            string searchPath = string.Empty;
            if (GetCommandLineArgs(commandlineArgs, ref searchPath, ref isErrorsWarningOnly)) // Set the filePath as the one sent to the application as a commandline parameter.
            {
                searchPath = commandlineArgs[0];
            }
            else
            {
                Console.WriteLine(Environment.ExpandEnvironmentVariables("Please pass a valid HTML/CSS file or directory to %PROGRAMNAME%."));
                Exit(EXIT_EMPTY_PATH);
            }

            bool isDir = IsDir(searchPath);
            if (!isDir && !IsFile(searchPath))
            {
                Console.WriteLine(Environment.ExpandEnvironmentVariables("Please pass a valid HTML/CSS file or directory to %PROGRAMNAME%."));
                Exit(EXIT_INVALID_PATH);
            }

            string[] fileList = new string[0]; // Create a new directory array.
            if (isDir)
            {
                fileList = Directory.GetFiles(searchPath, "*.*", SearchOption.AllDirectories);
                if (fileList.Length == 0) // If no files were found then exit with the exit code of EXIT_EMPTY_PATH.
                {
                    Exit(EXIT_EMPTY_PATH);
                }
            }
            else // Otherwise it's a file as a check was done before to check if the file path was either a directory or file.
            {
                string[] tempFileList = { searchPath }; // Workaround for when a single HTML/CSS file is passed to HTMLValid.
                fileList = tempFileList; // Set the directory array to the temporary array.
                tempFileList = null; // Set to null to destroy the temporary array.
            }

            if (fileList.Length > FILECHECK_THRESHOLD)
            {
                Console.WriteLine("The directory contains more than {0} files and could take anywhere between " +
                    "{1} second{2} to complete.\n" +
                    "\n" +
                    "This is a FREE service and thus W3C recommends a one second waiting period between uploads.\n" +
                    "\n" +
                    "Would you like to continue processing? (Y or N)", FILECHECK_THRESHOLD, fileList.Length, (fileList.Length > 1 ? "s" : ""));

                string userChoice = string.Empty;
                byte failCount = 0;
                while (userChoice != "y" && userChoice != "n") // Continue to loop until y or n is entered.
                {
                    if (failCount > 0) // If the fail count is greater than zero then display a warning as to what is to be entered.
                    {
                        Console.WriteLine(); // Empty line.
                        Console.Write("Please enter either Y or N: ");
                    }
                    userChoice = Console.ReadKey(true).KeyChar.ToString().ToLower(); // Get the char and convert to string and lowercase.
                    failCount++;
                }
                if (userChoice == "n")
                    Exit(EXIT_USER_CLOSE);
            }

            Console.WriteLine(); // Empty line.
            Console.WriteLine("Validating . . .");
            Console.WriteLine(); // Empty line.

            sbyte exitCode = EXIT_W3C_INVALID; // Set the exit code to the default EXIT_W3C_INVALID, which is zero in this case.
            int fileCount = 0, validCount = 0; // Set the file and valid count to zero.
            HTMLValid htmlValid = new HTMLValid(Environment.ExpandEnvironmentVariables("%PROGRAMNAME%")); // Create a HTMLValid object and set the UserAgent.

            Stopwatch totalTimer = Stopwatch.StartNew(); // Create a timer object.
            foreach (string filePath in fileList) // As the directory array isn't being written to then a foreach() is preferred.
            {
                HTMLValidResults htmlResults = htmlValid.ValidateFilePath(filePath, true); // Returns a HTMLValidResults object.
                if (htmlResults.FileType == HTMLValidFileType.NonSupportedFile) // If the file isn't a HTML or CSS file then skip the file.
                {
                    continue;
                }

                if (!htmlResults.IsConnected) // An error occurred with connecting to W3C so break from the loop.
                {
                    Console.WriteLine("An error occurred connecting to W3C's validation service.\n" +
                        "Please try again or contact your local administrator.");
                    exitCode = EXIT_W3C_CONNECTION;
                    break;
                }

                validCount += (htmlResults.Status == HTMLValidStatus.Valid ? 1 : 0);
                fileCount++;
                if (isErrorsWarningOnly && htmlResults.Errors == 0 && htmlResults.Warnings == 0) // If only display errors and warnings and no errors or warnings found then continue.
                {
                    continue;
                }

                string fileName = PathCompactPathEx(filePath, 15);
                Console.WriteLine("The {0} file {1} was {2}.", (htmlResults.FileType == HTMLValidFileType.CSS ? "CSS" : "HTML"), fileName, (htmlResults.Status == HTMLValidStatus.Valid ? "Valid" : "Invalid"));

                if (htmlResults.Errors > 0 || htmlResults.Warnings > 0) // Print out the errors and/or warnings.
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
                Console.WriteLine(); // Empty line.

                if (fileList.Length >= 2)
                {
                    Thread.Sleep(1000); // Recommendation by W3C is to sleep for 1 second, though this will only happen if there are 2 or more files.
                }
            }

            htmlValid = null; // Destroy the reference to HTMLValid().

            int seconds = (int)totalTimer.ElapsedMilliseconds / 1000; // Get the total number of elapsed seconds and cast as an integer.
            if (exitCode == EXIT_W3C_INVALID) // If equal to zero then no major error occurred during the loop.
            {
                exitCode = (validCount == fileList.Length ? EXIT_W3C_VALID : EXIT_W3C_INVALID); // If all files were valid then set the exit code to EXIT_VALID_HTML.
                Console.WriteLine("Created: {0}\n" +
                    "Files: {1}\n" +
                    "Valid: {2}\n" +
                    "Running: {3} second{4}.", DateTime.Now, fileCount, validCount, seconds, (seconds == 1 ? "" : "s"));
            }
            Exit(exitCode);
        }

        private static string PathCompactPathEx(string filePath)
        {
            return PathCompactPathEx(filePath, 25);
        }

        private static string PathCompactPathEx(string filePath, int length)
        {
            if (!string.IsNullOrEmpty(filePath) && (filePath.Length - Path.GetFileName(filePath).Length) > length)
            {
                filePath = Regex.Replace(filePath, @"^(.{" + length + @"}).+?([^\\]+)$", @"$1...\$2"); // Shorten the path but still retaining the file name length.
            }
            return filePath;
        }
    }
}