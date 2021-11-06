using System;
using System.Threading.Tasks;

namespace AnimeSD2HD
{
    internal class Void
    {
        public static Void Default { get; } = new Void();
    }

    internal interface IExternalProcess<Out, In>
    {
        event EventHandler<ProgressInfoViewModel> ProgressUpdate;
        event EventHandler<string> StandardOutputReceived;
        event EventHandler<string> StandardErrorReceived;

        Task<Out> Run(In args);
        Task Abort();
    };
}