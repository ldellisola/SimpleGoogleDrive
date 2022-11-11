// See https://aka.ms/new-console-template for more information

using System.Reflection;
using SimpleGoogleDrive;
using SimpleGoogleDrive.Models;

Console.WriteLine("Hello, World!");


var settings = new DriveAuthSettings(
    applicationName:    "GoogleDriveTools",
    credentials:        new FileInfo("./credentials.json"),
    userStore:          "GoogleDriveTools.Google.Auth",
    mode: DriveAuthSettings.AuthMode.Console
    );

using (var drive = new GoogleDriveService(settings, false,"./storage.json")){
    await drive.Authenticate();
    
    DriveResource? file = await drive.GetResource("1FuZJRV-9fr_LptreQQiN3F1xzRDGyrrx");

    var query = new QueryBuilder().IsNotType(DriveResource.MimeType.Folder).And().IsNotType(DriveResource.MimeType.GoogleDriveShortcut); //.And().HasNotPropertyValue("is downloaded","true");

    int aaa = 0;
    await foreach (var a in file.GetInnerResources(query,deepSearch:true))
    {
        aaa++;
        Console.WriteLine(await a.GetFullName());
    }
    Console.WriteLine(aaa);

    return;
    // DriveResource? file = await drive.FindFolder("/Shared");

    // var fullName = await file?.GetFullName()!;
    
    // Console.WriteLine(fullName);
    int aa = 0;
    
    // var query = new QueryBuilder();//.IsNotType(DriveResource.MimeType.Folder);
    
    await foreach (var item in file!.GetInnerResources(deepSearch: false))
    {
        aa++;
        var a = await item.GetFullName();
        Console.WriteLine($"{DateTime.Now.ToLongTimeString()}: {item.Id} : {a}");
    }
    
    Console.WriteLine($"ok. total: {aa} Calls {drive.Calls}");

    // var param = new QueryBuilder().IsOwner("user@gmail.com")
    //                                 .And(new QueryBuilder() .TypeContains("video")
    //                                                         .Or()
    //                                                         .TypeContains("photo")
    //                                 );
    //
    // IAsyncEnumerable<DriveResource> resources =  drive.QueryResources(param);
    //
    // DriveResource? filew = await drive.CreateFile(new FileInfo("local/path/to/file"), "remote/path/to/file");
    //
    //
    // file.Properties["new Property"] = "value";
    // file.Name = "New name";
    // await file.Update(new FileInfo("path/to/content"));
}