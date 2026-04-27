using DAL.Models;

namespace DAL;

public interface IPersonalityRepository
{
    void Initialize();
    IEnumerable<PersonalityEntry> GetAll();
    PersonalityEntry Insert(PersonalityEntry entry);
    void Update(PersonalityEntry entry);
}