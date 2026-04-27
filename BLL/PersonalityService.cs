using DAL;
using DAL.Models;

namespace BLL;

public interface IPersonalityService
{
    IEnumerable<PersonalityEntry> GetPersonalities();
    PersonalityEntry CreatePersonality(PersonalityEntry entry);
    void UpdatePersonality(PersonalityEntry entry);
}

public class PersonalityService : IPersonalityService
{
    private readonly IPersonalityRepository _repository;

    public PersonalityService(IPersonalityRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<PersonalityEntry> GetPersonalities()
    {
        return _repository.GetAll();
    }

    public PersonalityEntry CreatePersonality(PersonalityEntry entry)
    {
        return _repository.Insert(entry);
    }

    public void UpdatePersonality(PersonalityEntry entry)
    {
        _repository.Update(entry);
    }
}