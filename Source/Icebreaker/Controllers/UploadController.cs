//----------------------------------------------------------------------------------------------
// <copyright file="UploadController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker.Controllers
{
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Mvc;
    using Icebreaker.Helpers;

    /// <summary>
    /// Controller for image upload
    /// </summary>
    public class UploadController : Controller
    {
        private readonly ImageDataProvider ip = new ImageDataProvider();
        private readonly ImageService imageService = new ImageService();

        /// <summary>
        /// Upload Image Method
        /// </summary>
        /// <param name="userDetails">Gets User Details</param>
        /// <returns> View</returns>
        [HttpGet]
        public ActionResult Upload(string userDetails)
        {
            this.TempData["UserDetails"] = userDetails;
            return this.View();
        }

        /// <summary>
        /// Upload image using file uploader
        /// </summary>
        /// <param name="photo">Gets Uploaded Image</param>
        /// <returns>Return view </returns>
        [HttpPost]
        public async Task<ActionResult> Upload(HttpPostedFileBase photo)
        {
            if (photo != null)
            {
                var imageUrl = await this.imageService.UploadImageAsync(photo);
                string userDetails = this.TempData["UserDetails"].ToString();
                string[] splitString = userDetails.Split('-');
                string feedbackId = splitString[0].ToString();
                string feedbackfrom = splitString[1].ToString();
                string feedbackto = splitString[2].ToString();
                string imagedata = imageUrl.ToString();

                ImageInfo imageInfo = new ImageInfo
                {
                    Imageurl = imagedata,
                    ImageId = feedbackId,
                    PersonGivenFrom = feedbackfrom,
                    PersonGivenTo = feedbackto,
                };

                await this.ip.UpdateImageInfoAsync(imageInfo, true);
                return this.RedirectToAction("LatestImage", "Upload");
            }

            return this.View();
        }

        /// <summary>
        /// Latest Image
        /// </summary>
        /// <returns>File uploaded</returns>
        public ActionResult LatestImage()
        {
            this.ViewBag.Message = "File has been uploaded successfully";
            return this.View();
        }
    }
}