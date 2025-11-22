using FootballManager.Data.Entities;

namespace FootballManager.Business.Repositories
{
    public static class TransferHelper
    {
        public static int CalculateTransferPrice(Footballer player)
        {
            return player.ContractYears * player.Quality;
        }
    }

}
