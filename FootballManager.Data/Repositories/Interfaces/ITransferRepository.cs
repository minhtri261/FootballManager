using FootballManager.Data.Entities;

namespace FootballManager.Data.Repositories.Interfaces
{
    public interface ITransferRepository : IBaseRepository<Transfer>
    {
        Task<List<Footballer>> GetListFootballerCanTransferAsync();
        Task AddFreeAgentOfferAsync(int clubId, int footballerId, int contractYears);
        Task RenewContractAsync(int clubId, int footballerId, int additionalYears, decimal price);

        Task AddTransferOfferAsync(int fromClubId, int footballerId, int toClubId, int contractYears, decimal price);

        Task<bool> HasRenewOfferAsync(int footballerId, int clubId);

        Task AcceptTransferAsync(Transfer transfer, Footballer footballer);

        Task RejectTransferAsync(Transfer transfer);

        Task<List<Transfer>> GetPendingTransfersForClubAsync(int clubId, int footballerId);

        Task<List<Transfer>> GetPendingTransfersWithDetailsAsync();

    }

}
