namespace Ropoly.Application.Navigation
{
    /// <summary>
    /// Application-facing contract for changing scenes.
    /// The implementation is supplied by the Bootstrap composition root.
    /// </summary>
    public interface ISceneFlowService
    {
        bool IsLoading { get; }

        string ActiveSceneName { get; }

        bool TryLoad(string sceneName);
    }
}
