/******************************************************************************
 * Filename    = SubmissionModel.cs
 *
 * Author      = Sidharth Chadha
 * 
 * Project     = Cloud_Ux
 *
 * Description = Created Model for the downloading functionality. 
 *****************************************************************************/

using ServerlessFunc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Cloud_UX
{
    public class SubmissionsModel
    {
        //getting path from the files
        string[] paths;
        private string analysisUrl = "http://localhost:7074/api/analysis";
        private string submissionUrl = "http://localhost:7074/api/submission";
        private string sessionUrl = "http://localhost:7074/api/session";
        private DownloadApi fileDownloadApi; //creating an instance of the FiledowloadApi.

        public SubmissionsModel() //constructor for the submissionmodel class. 
        {
            fileDownloadApi = new DownloadApi(sessionUrl, submissionUrl, analysisUrl);
        }

        public IReadOnlyList<SubmissionEntity>? SubmissionsList; //creating the submission list to store the details of type submission model. 

        /// <summary>
        /// uses the async function to reterieve the file from the cloud. 
        /// </summary>
        /// <param name="sessionId">Unique id for a session</param>
        /// <returns>Returns the submission entity for given session id</returns>
        public async Task<IReadOnlyList<SubmissionEntity>> GetSubmissions(string sessionId, string userName)
        {
            //IReadOnlyList<SubmissionEntity>? getEntity = await fileDownloadApi.GetFilesBySessionIdAsync(sessionId);
            IReadOnlyList<SubmissionEntity>? getEntity = null;
            SubmissionsList = getEntity;
            return getEntity;
        }

        /// <summary>
        /// For getting the path of user with respect to their local system.. 
        /// </summary>
        /// <returns>Return a path to download folder</returns>
        public static string GetDownloadFolderPath() //Getting the path to folder where the downloads folder contains. 
        {
            return System.Convert.ToString(
                Microsoft.Win32.Registry.GetValue(
                     @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders"
                    , "{374DE290-123F-4565-9164-39C4925E467B}"
                    , String.Empty
                )
            );
        }


    }
}
