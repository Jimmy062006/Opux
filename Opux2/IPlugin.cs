using System;
using System.Threading.Tasks;

namespace Opux2
{
    public interface IPlugin
    {
        string Name { get; }
        string Description { get; }
        string Author { get; }
        Version Version { get; }
        Task OnLoad();
        Task Pulse();
    }
}