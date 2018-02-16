using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Opux2
{
    public static class PluginLoader<T>
    {
        public static ICollection<T> LoadPlugins(string path)
        {
            string[] dllFileNames = null;
            string[] pluginFolderNames = null;
            ICollection<T> plugins = new List<T>();

            string currentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            if (Directory.Exists(Path.Combine(currentDirectory, path)))
            {

                pluginFolderNames = Directory.GetDirectories(Path.Combine(currentDirectory, path));

                foreach (var d in pluginFolderNames)
                {
                    dllFileNames = Directory.GetFiles(d, "*.dll");

                    ICollection<Assembly> assemblies = new List<Assembly>(dllFileNames.Length);
                    foreach (string dllFile in dllFileNames)
                    {
                        AssemblyName an = AssemblyName.GetAssemblyName(dllFile);
                        Assembly assembly = Assembly.LoadFile(Path.Combine(currentDirectory, dllFile));
                        assemblies.Add(assembly);
                        Logger.DiscordClient_Log(new Discord.LogMessage(Discord.LogSeverity.Info, "PluginLoader", $"Loading Plugin {an.Name}"));
                    }


                    Type pluginType = typeof(T);
                    ICollection<Type> pluginTypes = new List<Type>();
                    foreach (Assembly assembly in assemblies)
                    {
                        if (assembly != null)
                        {
                            Type[] types = assembly.GetTypes();

                            foreach (Type type in types)
                            {
                                if (type.IsInterface || type.IsAbstract)
                                {
                                    continue;
                                }
                                else
                                {
                                    if (type.GetInterface(pluginType.FullName) != null)
                                    {
                                        pluginTypes.Add(type);
                                    }
                                }
                            }
                        }
                    }


                    foreach (Type type in pluginTypes)
                    {
                        T plugin = (T)Activator.CreateInstance(type);
                        plugins.Add(plugin);
                    }
                }

                return plugins;
            }

            return null;
        }
    }
}
