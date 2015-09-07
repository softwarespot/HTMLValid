using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace HTMLValid
{
    /// <summary>
    ///     HTMLValid class
    /// </summary>
    class HtmlValid
    {
        /// <summary>
        ///     Private variable to hold the user agent string
        /// </summary>
        private string _userAgent;

        /// <summary>
        ///     User agent string property
        /// </summary>
        public string UserAgent
        {
            get
            {
                return _userAgent;
            }
            set
            {
                // If not null or whitespace then set the user agent
                _userAgent = !String.IsNullOrWhiteSpace(value) ? value : "HTMLValid";
            }
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        public HtmlValid()
        {
            // Set the user agent with the default value
            UserAgent = null;
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="userAgent">Valid user agent string</param>
        public HtmlValid(string userAgent)
        {
            UserAgent = userAgent;
        }

        /// <summary>
        ///     Validate a filepath
        /// </summary>
        /// <param name="filePath">Filepath to validate</param>
        /// <param name="isReportSource">Output the report</param>
        /// <returns>HtmlValidResults structure</returns>
        public HtmlValidResults ValidateFilePath(string filePath, bool isReportSource)
        {
            // Values that will later be passed to HTMLValidResults()
            bool isConnected = false;
            int errors = 0;
            int warnings = 0;
            HtmlValidFileType fileType = HtmlValidFileType.NonSupportedFile;
            HtmlValidStatus status = HtmlValidStatus.Abort;

            bool isCss = IsCssFile(filePath);
            bool isHtml = IsHtmlFile(filePath);

            if (isCss || isHtml)
            {
                string fileParams = File.ReadAllText(filePath, Encoding.UTF8); // Read the file using UTF8 encoding
                fileParams = Uri.EscapeDataString(fileParams); // Encode the file data by escaping certain characters

                HttpWebResponse webResponse;
                if (isCss)
                {
                    fileType = HtmlValidFileType.Css;
                    fileParams = "?text=" + fileParams + (isReportSource ? "&ss=1" : "");
                    webResponse = HttpGet(@"http://jigsaw.w3.org/css-validator/validator", fileParams);
                }
                else
                {
                    fileType = HtmlValidFileType.Html;
                    fileParams = "uploaded_file=" + fileParams + (isReportSource ? "&output=text" : "");
                    webResponse = HttpPost(@"http://validator.w3.org/check", fileParams);
                }

                if (webResponse == null)
                {
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    return new HtmlValidResults(isConnected, status, fileType, errors, warnings);
                }

                using (webResponse)
                {
                    // Set the IsOnline property
                    isConnected = IsOnline(webResponse.StatusCode);

                    // Parsing the source is a better approach, but for some reason the regular expressions were causing an exception
                    // Set the number of errors field
                    Int32.TryParse(webResponse.GetResponseHeader("X-W3C-Validator-Errors"), out errors);
                    switch (webResponse.GetResponseHeader("X-W3C-Validator-Status").ToLower())
                    {
                        case "valid":
                            status = HtmlValidStatus.Valid;
                            break;

                        case "invalid":
                            status = HtmlValidStatus.Invalid;
                            break;

                        case "abort":
                            status = HtmlValidStatus.Abort;
                            break;

                        default:
                            status = HtmlValidStatus.Abort;
                            break;
                    }

                    // Only returned when validating a HTML file
                    if (isHtml)
                    {
                        // Set the number of warnings field
                        Int32.TryParse(webResponse.GetResponseHeader("X-W3C-Validator-Warnings"), out warnings);
                    }
                }
            }

            return new HtmlValidResults(isConnected, status, fileType, errors, warnings);
        }

        /// <summary>
        ///     Create a a HTTP GET request
        /// </summary>
        /// <param name="url">Resource URL</param>
        /// <param name="parameters">Query parameter(s)</param>
        /// <returns>HttpWebResponse on success; otherwise, null on failure</returns>
        private HttpWebResponse HttpGet(string url, string parameters)
        {
            HttpWebResponse webResponse = null;
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url + parameters);
                webRequest.Method = "GET";
                webRequest.UserAgent = UserAgent;
                webResponse = (HttpWebResponse)webRequest.GetResponse();
            }
            catch
            {
                // Ignored
            }

            return webResponse;
        }

        private HttpWebResponse HttpPost(string url, string parameters)
        {
            HttpWebResponse webResponse = null;
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Method = "POST";
                webRequest.UserAgent = UserAgent;

                // Create a byte array of data to write to the request stream
                byte[] postData = Encoding.UTF8.GetBytes(parameters);

                // Set the Content-Length of the header
                webRequest.ContentLength = postData.Length;

                using (Stream webStream = webRequest.GetRequestStream())
                {
                    webStream.Write(postData, 0, postData.Length);
                }

                // Get the response from the server
                webResponse = (HttpWebResponse)webRequest.GetResponse();
            }
            catch
            {
                // Ignored
            }

            return webResponse;
        }

        /// <summary>
        ///     Check if a filepath is a CSS file i.e. ends with .css
        /// </summary>
        /// <param name="filePath">Filepath to check</param>
        /// <returns>Ture the filepath is a CSS file; otherwise, false</returns>
        private static bool IsCssFile(string filePath)
        {
            return File.Exists(filePath) && filePath.EndsWith(".css", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     Check if a filepath is a HTML file i.e. ends with .htm or .html
        /// </summary>
        /// <param name="filePath">Filepath to check</param>
        /// <returns>True the filepath is a HTML file; otherwise, false</returns>
        private static bool IsHtmlFile(string filePath)
        {
            return File.Exists(filePath) && (filePath.EndsWith(".htm", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///     Check if a status code matches a success HTTP status code
        /// </summary>
        /// <param name="statusCode">Status code to check</param>
        /// <returns>True the status code was successful; otherwise, false</returns>
        private bool IsOnline(HttpStatusCode statusCode)
        {
            HttpStatusCode[] httpGoodStatuses = { HttpStatusCode.OK,
                                                    HttpStatusCode.Moved,
                                                    HttpStatusCode.Redirect,
                                                    HttpStatusCode.RedirectMethod,
                                                    HttpStatusCode.NotModified,
                                                    HttpStatusCode.RedirectKeepVerb,
                                                    HttpStatusCode.Unauthorized,
                                                    HttpStatusCode.Forbidden,
                                                    HttpStatusCode.MethodNotAllowed };

            return httpGoodStatuses.Any(httpGoodStatus => statusCode == httpGoodStatus);
        }

        /// <summary>
        ///     Check if the value is a valid Uniform Resource Locator (URL)
        /// </summary>
        /// <param name="value">Value to check</param>
        /// <returns>True the value if a valid URL; otherwise, false</returns>
        private bool IsUrl(string value)
        {
            Uri urlResult;

            return Uri.TryCreate(value, UriKind.Absolute, out urlResult) && urlResult.Scheme == Uri.UriSchemeHttp;
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        /// <summary>
        ///     Validate a Uniform Resource Locator (URL)
        /// </summary>
        /// <param name="urlPath">URL to check</param>
        /// <param name="isReportSource">Output the report</param>
        /// <returns>HtmlValidResults structure</returns>
        private HtmlValidResults ValidateUrl(string urlPath, bool isReportSource)
        {
            // Values that will later be passed to HTMLValidResults()
            const bool isConnected = false;
            const int errors = 0;
            const int warnings = 0;
            HtmlValidFileType fileType = HtmlValidFileType.NonSupportedFile;
            const HtmlValidStatus status = HtmlValidStatus.Abort;

            if (IsUrl(urlPath))
            {
                fileType = HtmlValidFileType.Url;
                // Escape the URL
                // ReSharper disable once UnusedVariable
                string fileData = "uri=" + Uri.EscapeUriString(urlPath);
            }

            return new HtmlValidResults(isConnected, status, fileType, errors, warnings);
        }
    }
}
