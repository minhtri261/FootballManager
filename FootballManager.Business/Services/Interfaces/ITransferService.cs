using FootballManager.Business.DTOs;
using FootballManager.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManager.Business.Services.Interfaces
{
    public interface ITransferService : IBaseService<Transfer>
    {
        Task<IEnumerable<FootballerDto>> GetListFootballerCanTransferAsync();
        Task<IEnumerable<Transfer>> GetPendingTransfersForClubAsync(int footballerId);
        Task SendOfferToFreeAgentAsync(int clubId, int footballerId, int contractYears);
        Task RenewContractAsync(int clubId, int footballerId, int additionalYears);

        Task SendTransferOfferAsync(int fromClubId, int footballerId, int toClubId, int contractYears);

        Task BotDecideTransfersAsync();
        Task BotDecideWhoToBuyOrRenewAsync();

        // Bot tự niêm yết cầu thủ dư thừa (thừa so với target squad composition) lên thị trường chuyển nhượng
        Task BotListSurplusPlayersAsync();

    }

}
