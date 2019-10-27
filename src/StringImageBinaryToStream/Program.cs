using HtmlAgilityPack;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;

namespace StringImageBinaryToStream
{
	public static class Program
	{

		// This code sample uses the following NuGet packages:
		// HTMLAgilityPack v1.11.16
		// Microsoft.Azure.Storage.Blob 11.1.0


		// In a real-world application, these settings would be in a more secure location
		// This is done here to make the sample simpler
		private const string settings_StorageConnectionString = "{Connection string for your storage account}";
		private const string settings_StorageContainerName = "{Blob container where to upload images to}";
		private const string settings_StorageMasterFolderName = "{Folder within the blob container to upload images to}";

		public static void Main()
		{
			ProcessAsync().GetAwaiter().GetResult();
		}

		private static async Task ProcessAsync()
		{
			string folderName = Guid.NewGuid().ToString();
			string input = File.ReadAllText(@"D:\Repos\Blog\StringImageBinaryToStream\src\SampleInput.txt");

			Console.WriteLine(await ProcessHtmlInput(folderName, input).ConfigureAwait(true));
		}

		/// <summary>Processes the HTML input.</summary>
		/// <param name="folderName">Name of the blob container folder where to store any embedded images.</param>
		/// <param name="htmlInput">The HTML input to be parsed.</param>
		/// <returns>Something</returns>
		public static async Task<string> ProcessHtmlInput(string folderName, string htmlInput)
		{

			// Load the HTML
			var htmlDocument = new HtmlDocument();
			htmlDocument.LoadHtml(htmlInput);

			// Process the image tags in the input HTML
			var htmlNodes = htmlDocument.DocumentNode.SelectNodes("//img");
			if (htmlNodes != null)
			{
				foreach (var node in htmlNodes)
					node.SetAttributeValue("src", await ProcessImageAsync(node, folderName).ConfigureAwait(true));
			}

			// Write out the resulting HTML
			string returnValue;
			using (StringWriter stringWriter = new StringWriter())
			{
				htmlDocument.Save(stringWriter);
				returnValue = stringWriter.ToString();
			}
			return returnValue;

		}

		/// <summary>Processes the image asynchronous.</summary>
		/// <param name="imageNode">The image node to be processed.</param>
		/// <param name="folderName">Name of the blob container folder where to store the image.</param>
		/// <returns>The updated image src attribute value containing the URL of the image instead of a binary string.</returns>
		private static async Task<string> ProcessImageAsync(HtmlNode imageNode, string folderName)
		{
			if (imageNode.GetAttributeValue("src", "").StartsWith("data:image/", StringComparison.InvariantCultureIgnoreCase))
			{
				string attributeValue = imageNode.GetAttributeValue("src", "");
				string fileExtension = attributeValue[11..attributeValue.IndexOf(";", StringComparison.InvariantCultureIgnoreCase)];
				string binaryString = attributeValue.Substring(attributeValue.IndexOf(",", StringComparison.InvariantCultureIgnoreCase) + 1);
				imageNode.SetAttributeValue("src", await SaveImageAsync(folderName, fileExtension, binaryString).ConfigureAwait(true));
			}
			return imageNode.GetAttributeValue("src", "");
		}

		/// <summary>Saves the image asynchronous.</summary>
		/// <param name="folderName">Name of the blob container folder where to put the image.</param>
		/// <param name="fileExtension">Extension of the file to be stored.</param>
		/// <param name="image">The binary string containing the image.</param>
		/// <returns>The URL of the blob containing the saved image.</returns>
		/// <exception cref="Exception">
		/// Thrown if unable to connect to the storage account or there was an error thrown while uploading the image to Azure Storage.
		/// In the second case, review the inner exception to get details of what went wrong.
		/// </exception>
		private static async Task<string> SaveImageAsync(string folderName, string fileExtension, string image)
		{

			string imageFileName = $"{Guid.NewGuid().ToString().Replace("-", "", StringComparison.InvariantCultureIgnoreCase)}.{fileExtension}";

			if (CloudStorageAccount.TryParse(settings_StorageConnectionString, out CloudStorageAccount storageAccount))
			{

				try
				{
					CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
					CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(settings_StorageContainerName);

					var bytes = Convert.FromBase64String(image);

					Uri blobUri;
					using (MemoryStream stream = new MemoryStream(bytes))
					{
						CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference($"{settings_StorageMasterFolderName}/{folderName}/{imageFileName}");
						cloudBlockBlob.Properties.ContentType = $"image/{fileExtension}";
						await cloudBlockBlob.UploadFromStreamAsync(stream).ConfigureAwait(true);
						blobUri = cloudBlockBlob.Uri;
					}

					return blobUri.ToString();

				}
				catch (Exception ex)
				{
					throw new Exception("Failed to save image to container.", ex);
				}

			}
			else
			{
				throw new Exception("Unable to connect to the storage account");
			}
		}

	}

}