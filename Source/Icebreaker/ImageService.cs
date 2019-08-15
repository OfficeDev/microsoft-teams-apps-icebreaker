// <copyright file="ImageService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Hosting;
    using Microsoft.Azure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

/// <summary>
/// Builder class for Image Upload
/// </summary>
    public class ImageService
    {
        /// <summary>
        /// Uploads image to blob
        /// </summary>
        /// <param name="imageToUpload">Images will be Uploaded</param>
        /// <returns> Return Image path</returns>
        public async Task<string> UploadImageAsync(HttpPostedFileBase imageToUpload)
        {
            string imageFullPath = null;
            if (imageToUpload == null || imageToUpload.ContentLength == 0)
            {
                return null;
            }

            try
            {
                string fileName = Path.GetFileName(imageToUpload.FileName);
                CloudStorageAccount cloudStorageAccount = ConnectionString.GetConnectionString();
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("photos");

                if (await cloudBlobContainer.CreateIfNotExistsAsync())
                {
                    await cloudBlobContainer.SetPermissionsAsync(
                        new BlobContainerPermissions
                        {
                            PublicAccess = BlobContainerPublicAccessType.Blob
                        });
                }

                string imageName = Guid.NewGuid().ToString() + "-" + Path.GetExtension(imageToUpload.FileName);

                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(imageName);
                cloudBlockBlob.Properties.ContentType = imageToUpload.ContentType;
                await cloudBlockBlob.UploadFromStreamAsync(imageToUpload.InputStream);

                imageFullPath = cloudBlockBlob.Uri.ToString();
            }
            catch (Exception)
            {
            }

            return imageFullPath;
        }

        /// <summary>
        /// Retrieve connection string.
        /// </summary>
        public static class ConnectionString
        {
            private static string account = CloudConfigurationManager.GetSetting("StorageAccountName");
            private static string key = CloudConfigurationManager.GetSetting("StorageAccountKey");

            /// <summary>
            /// GetConnectionString get connection string
            /// </summary>
            /// <returns>Return connection string of blob storage</returns>
            public static CloudStorageAccount GetConnectionString()
            {
                string connectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", account, key);
                return CloudStorageAccount.Parse(connectionString);
            }
        }
    }
}
