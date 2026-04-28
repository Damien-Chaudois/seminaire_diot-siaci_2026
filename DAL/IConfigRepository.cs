using DAL.Models;

namespace DAL;

public interface IConfigRepository
{
    void Initialize();
    string Get(string key);
    void Set(string key, string value);
}
