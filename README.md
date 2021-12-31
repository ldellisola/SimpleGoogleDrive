# SimpleGoogleDrive
Using the Google Drive library currently available is a mess. The lack of documentation for .Net is horrible and it just makes development much more dificult.

I made this library mostly to aid me on my own projects, you can take a look at them [here](https://github.com/ldellisola/GoogleDriveTools). 
It servers mostly for basic actions, such as basic queries, creating, updating and deleting files, as well as downloading their contents.

## Usage
### Authenticating
```csharp
var settings = new DriveAuthorizationSettings(
    applicationName:    "AppName",
    credentials:        new FileInfo("Path/to/credentials.json"),
    userStore:          "User.Store.Name"
    );

using (var drive = new GoogleDriveService(settings, true, "Path/To/Persistant/Storage.json")){
    await drive.Authenticate();
    
    // Code....
}
```

### Queries
#### Find File

We can add extra parameters to our file search, but it is not mandatory. In this example we are retrieveing a file by its name
```csharp
DriveResource? file = await drive.FindFile("remote/path/to/file");
```

#### Find Folder

We can add extra parameters to our file search, but it is not mandatory. In this example we are retrieveing a folder by its name
```csharp
DriveResource? file = await drive.FindFolder("remote/path/to/folder");
```

#### Query Resources
If we want to get all the files that are pictures or videos and are owned by a specific user

```csharp
var param = new QueryBuilder().IsOwner("user@gmail.com")
                              .And(new QueryBuilder() .TypeContains("video")
                                                      .Or()
                                                      .TypeContains("photo")
                              );

IEnumerable<DriveResource> resources = await drive.QueryResources(param);
```

### Create Resources
#### Create File
It will create a file in the specific path on Google Drive. If the folders in the path does not exists, then it wil create them
```csharp
DriveResource? file = await drive.CreateFile(new FileInfo("local/path/to/file"), "remote/path/to/file");
```
#### Create Folder
It will create a folder in the specific path on Google Drive
```csharp
DriveResource? folder = await drive.CreateFolder("remote/path/to/folder");
```

### Update Resources
We can either update the file name and properties, or also update the file contents
```csharp
file.Properties["new Property"] = "value";
file.Name = "New name";
await file.Update(new FileInfo("path/to/content"));
```

