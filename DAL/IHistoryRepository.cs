using DAL.Models;

namespace DAL;

public interface IHistoryRepository
{
    void Initialize();
    void Insert(HistoryEntry entry);
    IEnumerable<HistoryEntry> GetAll();
    void Delete(int id);
}
