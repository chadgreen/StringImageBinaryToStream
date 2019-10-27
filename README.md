# StringImageBinaryToStream
It is possible to have HTML image tags to have a data src attribute which has the image represented by a binary string instead of the standard URL.  The problem with this is that the whole HTML document (including the images) must be downloaded altogether and you lose the ability to cache those images.

In this sample, an inputted HTML block has its image nodes parsed.  If any of those image nodes contain a data src attribute, the binary string is converted to a stream, that stream is uploaded to Azure Storage, and then the src attribute is converted to standard URL pointer.

Something to note:  When building this for my project, I needed any of these images to go into a specific folder in the container and then a subfolder for each HTML document.  This was being done because the Storage container is used for other purposes and we wanted better organization.  If you do not have this need, you can remove the settings_StorageMasterFolderName and/or folderName references through the code.

This sample is to support the following blog article: [Converting an image binary in string to a stream](https://www.chadgreen.com/2019/10/27/converting-an-image-binary-in-string-to-a-stream/).