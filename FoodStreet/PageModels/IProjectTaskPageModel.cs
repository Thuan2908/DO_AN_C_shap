using CommunityToolkit.Mvvm.Input;
using FoodStreet.Models;

namespace FoodStreet.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}