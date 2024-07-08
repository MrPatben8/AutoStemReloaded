using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using UnityEngine;

public class PostBuildProcessor : IPostprocessBuildWithReport
{
	// Implement the order in which the processor will be called
	public int callbackOrder => 0;

	// This method is called after the build process is complete
	public void OnPostprocessBuild(BuildReport report)
	{
		// Define the source and destination directories
		string sourceDirectory = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), "tools");//Path.Combine(Application.dataPath, "tools");

		// Determine the target data folder based on the build output path
		string buildFolder = Path.GetDirectoryName(report.summary.outputPath);
		string dataFolder = Path.Combine(buildFolder, $"{Path.GetFileNameWithoutExtension(report.summary.outputPath)}_Data");
		string destinationDirectory = Path.Combine(dataFolder, "tools");

		// Check if the source directory exists
		if(Directory.Exists(sourceDirectory))
		{
			// Copy the tools directory to the build output directory
			CopyDirectory(sourceDirectory, destinationDirectory);
			Debug.Log("Tools folder copied to build directory.");
		} else
		{
			Debug.LogWarning("Tools folder not found in Assets directory.");
		}
	}

	// Helper method to copy all files and directories
	private void CopyDirectory(string sourceDir, string destDir)
	{
		// Create the destination directory if it doesn't exist
		Directory.CreateDirectory(destDir);

		// Copy all files from the source to the destination directory
		foreach(var file in Directory.GetFiles(sourceDir))
		{
			string destFile = Path.Combine(destDir, Path.GetFileName(file));
			File.Copy(file, destFile, true);
		}

		// Recursively copy all subdirectories
		foreach(var subDir in Directory.GetDirectories(sourceDir))
		{
			string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
			CopyDirectory(subDir, destSubDir);
		}
	}
}
