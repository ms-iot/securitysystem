var env = require('node-env-file');
env('../.env')
var azure = require('azure-storage');
var blobService = azure.createBlobService();


blobService.createBlockBlobFromLocalFile('imagecontainer', '635702506615122396_1.jpg', '../public/images/635702506615122396_1.jpg', function(error, result, response){
  if(!error){
    console.log('result', result)
    console.log('response', response)
  } else {
    console.log('error', error)
  }
})