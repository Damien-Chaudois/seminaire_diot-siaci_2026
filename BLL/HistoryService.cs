using DAL;
using DAL.Models;

namespace BLL;

public interface IHistoryService
{
    void SaveEntry(HistoryEntry entry);
    IEnumerable<HistoryEntry> GetHistory();
    void DeleteEntry(int id);
}

public class HistoryService : IHistoryService
{
    private readonly IHistoryRepository _repository;

    public HistoryService(IHistoryRepository repository)
    {
        _repository = repository;
    }

    public void SaveEntry(HistoryEntry entry)
    {
        _repository.Insert(entry);
    }

    public IEnumerable<HistoryEntry> GetHistory()
    {
        return _repository.GetAll();
    }

    public void DeleteEntry(int id)
    {
        _repository.Delete(id);
    }
}
