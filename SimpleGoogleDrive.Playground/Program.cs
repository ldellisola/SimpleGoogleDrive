// See https://aka.ms/new-console-template for more information
using SimpleGoogleDrive;
using SimpleGoogleDrive.Models;

Console.WriteLine("Hello, World!");


var settings = new DriveAuthorizationSettings(
    applicationName:    "AppName",
    credentials:        new FileInfo("Path/to/credentials.json"),
    userStore:          "User.Store.Name"
    );

using (var drive = new GoogleDriveService(settings, true, "Path/To/Persistant/Storage.json")){
    await drive.Authenticate();


    DriveResource? file = await drive.FindFile("remote/path/to/file");

    var param = new QueryBuilder().IsOwner("user@gmail.com")
                                    .And(new QueryBuilder() .TypeContains("video")
                                                            .Or()
                                                            .TypeContains("photo")
                                    );

    IEnumerable<DriveResource> resources = await drive.QueryResources(param);

    DriveResource? filew = await drive.CreateFile(new FileInfo("local/path/to/file"), "remote/path/to/file");


    file.Properties["new Property"] = "value";
    file.Name = "New name";
    await file.Update(new FileInfo("path/to/content"));
}