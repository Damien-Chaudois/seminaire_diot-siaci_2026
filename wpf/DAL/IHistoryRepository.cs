using wpf.Models;

namespace wpf.DAL;

public interface IHistoryRepository
{
    void Initialize();
    void Insert(HistoryEntry entry);
    IEnumerable<HistoryEntry> GetAll();
    void Delete(int id);
}
