using System.Threading;
using System.Threading.Tasks;

namespace InvoiceImporter.Application
{
    public interface IDataImporter
    {
        Task ImportData(string filePath, CancellationToken cancellationToken);
    }
}
