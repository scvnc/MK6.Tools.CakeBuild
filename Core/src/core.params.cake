#tool "nuget:?package=GitVersion.CommandLine"


public class BuildParams
{
	private const string solutionPathKey = "solutionPath";
	private const string targetKey = "target";
	private const string configurationKey = "configuration";
	private const string versionKey = "appVer";
	private const string packageVersionKey = "packVer";
	private const string companyKey = "company";
	private const string nuspecPathsKey = "nuspecPaths";

	public string SolutionPath { get; set; }
	public string Target { get; set; }
	public string Configuration { get; set; }
	public string Version { get; set; }
	public string PackageVersion { get; set; }
	public GitVersion GitVersion { get; set; }
	public string Company { get; set; }
	public FilePathCollection NuspecPaths { get; set; }
  

	public static BuildParams GetParams(ICakeContext context)
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}

		string solutionPath = null;
		if(!context.HasArgument(solutionPathKey))
		{
			context.Information("Attempting to load default .sln file...");
			var slnFiles = context.Globber.GetFiles("*.sln").ToArray();
			switch(slnFiles.Length)
			{
			case 0:
				context.Information("No .sln files found in the source root.");
				//throw new Exception("No .sln files found in the source root. Please specify the argument \"" + solutionPathKey + "\"");
				//return null;
				break;
			case 1:
				solutionPath = slnFiles[0].FullPath;
				context.Information("Found " + solutionPath);
				break;
			default:
				throw new Exception("More than 1 .sln file found in the source root. Please specify the argument \"" + solutionPathKey + "\"");
				return null;
			}
		}
		else
			solutionPath = context.Argument<string>(solutionPathKey);
    
		FilePathCollection nuspecPaths;
		if(context.HasArgument(nuspecPathsKey))
			nuspecPaths = new FilePathCollection(context.Argument<string>(nuspecPathsKey).Split(';').Select(x => new FilePath(x)), new PathComparer(context.Environment));
			else
			nuspecPaths = FindNuspecPaths(context);

		var version = context.Argument(versionKey, string.Empty);
		var packVersion = context.Argument<string>(packageVersionKey, version);
		GitVersion gitVersion = null;
		if(String.IsNullOrEmpty(version))
		{
			var slnFilePath = context.File(solutionPath).Path;
			if(slnFilePath.IsRelative)
				slnFilePath = slnFilePath.MakeAbsolute(context.Environment);

			var workingDir = slnFilePath.GetDirectory();
			context.Information("Setting GitVersion WorkingDirectory to {0}", workingDir.FullPath);
      gitVersion = context.GitVersion(new GitVersionSettings 
			{ 
				UpdateAssemblyInfo = false,
				WorkingDirectory = workingDir 
			});
			version = gitVersion.SemVer;
			packVersion = gitVersion.NuGetVersion;
		}

		return new BuildParams {
			SolutionPath = solutionPath,
			Target = context.Argument(targetKey, "Default"),
			Configuration = context.Argument(configurationKey, "Release"),
			Version = version,
			PackageVersion = packVersion,
			GitVersion = gitVersion,
			NuspecPaths = nuspecPaths
		};
	}

	private static FilePathCollection FindNuspecPaths(ICakeContext context)
	{
		Func<IFileSystemInfo, bool> excludePackagesFunc = fileSystemInfo => !fileSystemInfo.Path.FullPath.EndsWith("packages", StringComparison.OrdinalIgnoreCase);
    
		var nuspecs = context.Globber.GetFiles("**/*.nuspec");
		foreach(var nuspec in nuspecs)
		{
			context.Information("Found Nuspec: {0}", nuspec);
		}

		return new FilePathCollection(nuspecs, new PathComparer(context.Environment));
	}
  
}