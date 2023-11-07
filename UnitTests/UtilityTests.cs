using ServerlessFunc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass()]
    public class UtilityTests
    {
        [TestMethod()]
        public async Task BlobUtilityTest()
        {
            string blobName = "testblob";
            byte[] blobcontent = Encoding.ASCII.GetBytes("demotext");
            string containerName = "democontainer";
            string connectionString = "UseDevelopmentStorage=true";
            await BlobUtility.UploadSubmissionToBlob(blobName, blobcontent, connectionString, containerName );
            byte[] getBlobContent = await BlobUtility.GetBlobContentAsync(containerName, blobName,connectionString);
            await BlobUtility.DeleteContainer(containerName, connectionString);
            CollectionAssert.AreEqual(blobcontent, getBlobContent);
        }
    }
}
