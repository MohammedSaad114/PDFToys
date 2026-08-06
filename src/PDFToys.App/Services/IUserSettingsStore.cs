using PDFToys.App.Models;

namespace PDFToys.App.Services;

public interface IUserSettingsStore
{
    UserSettings Load();

    void Save(UserSettings settings);
}
