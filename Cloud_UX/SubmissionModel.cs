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
            // Call the API to get the submission bytes
            byte[] submissionBytes = await fileDownloadApi.GetSubmissionByUserNameAndSessionIdAsync(userName, sessionId);

            // If the submissionBytes is null, return an empty list
            if (submissionBytes == null)
            {
                return new List<SubmissionEntity>();
            }

            // Convert the bytes to a SubmissionEntity object
            SubmissionEntity submissionEntity = new SubmissionEntity(sessionId, userName);

            // You need to define a method to convert the byte array to your SubmissionEntity. Assuming a method called ConvertBytesToSubmissionEntity.
            // submissionEntity = ConvertBytesToSubmissionEntity(submissionBytes);

            // Assuming SubmissionsList is a property of your class
            SubmissionsList = new List<SubmissionEntity> { submissionEntity };

            return SubmissionsList;
        }

        


    }
}
