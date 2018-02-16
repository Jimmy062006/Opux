using System.IO;

namespace BuildEvents
{
    class Program
    {
        static void Main(string[] args)
        {
            var ProjectName = args[0];
            var SolutionDir = args[1];
            var TargetDir = args[2];

            if (Directory.Exists($"{SolutionDir}Opux2/bin/Debug/netcoreapp2.0/Plugins/{ProjectName}"))
                Directory.Delete($"{SolutionDir}Opux2/bin/Debug/netcoreapp2.0/Plugins/{ProjectName}", true);
            Directory.CreateDirectory($"{SolutionDir}Opux2/bin/Debug/netcoreapp2.0/Plugins/{ProjectName}/");

            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(TargetDir, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(TargetDir, $"{SolutionDir}Opux2/bin/Debug/netcoreapp2.0/Plugins/{ProjectName}/"));

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(TargetDir, "*.*", SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(TargetDir, $"{SolutionDir}Opux2/bin/Debug/netcoreapp2.0/Plugins/{ProjectName}/"), true);
        }
    }
}
