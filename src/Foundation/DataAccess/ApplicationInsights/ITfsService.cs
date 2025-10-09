using System.Threading.Tasks;

namespace DeanOBrien.Foundation.DataAccess.ApplicationInsights.ApplicationInsights
{
    public interface ITfsService
    {
        void Initialize(string organization, string project, string repoId, string pat);
        Task<string> GetFile(string fileName);
        Task<int> CreateBacklog(string content, string problemId);
        Task<bool> CreateBranchAndAddCommit(string newBranch, string filePath, string fileContent);
        Task<string> PostToApi(string url, string json);
        Task<string> GetFromApi(string url, string json);
        Task<int> GetWorkItemId(string problemId);
        Task<string> GetWorkItemDescription(int id);
        Task<int> CreatePR(string problemId, string sourceBranch, string targetBranch, int workItem = 0);
    }
}