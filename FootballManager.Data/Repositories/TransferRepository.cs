using FootballManager.Business.Repositories;
using FootballManager.Data.Entities;
using FootballManager.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Data.Repositories
{
    public class TransferRepository : BaseRepository<Transfer>, ITransferRepository
    {
        public TransferRepository(FootballContext context) : base(context) { }

        //Lấy danh sách cầu thủ đang được bán trên thị trường chuyển nhượng.
        public async Task<List<Footballer>> GetListFootballerCanTransferAsync()
        {
            return await _context.Footballers
                .Include(f => f.Club)
                .Where(f => f.IsTransferListed && (f.ClubId == null || !f.Club.IsFinalized))
                .ToListAsync();
        }

        //Gửi lời đề nghị chiêu mộ cầu thủ tự do
        public async Task AddFreeAgentOfferAsync(int clubId, int footballerId, int contractYears)
        {
            var footballer = await _context.Footballers.FindAsync(footballerId);
            if (footballer == null || !footballer.IsTransferListed)
                throw new Exception("Cầu thủ này không thể chuyển nhượng lúc này.");

            var transfer = new Transfer
            {
                FootballerId = footballerId,
                FromClubId = null,
                ToClubId = clubId,
                TransferFee = 0,
                ContractYears = contractYears,
                Status = "Pending"
            };

            await _context.Transfers.AddAsync(transfer);
            await _context.SaveChangesAsync();
        }

        //Gia hạn hợp đồng cho cầu thủ
        public async Task RenewContractAsync(int clubId, int footballerId, int additionalYears)
        {
            var footballer = await _context.Footballers
                .Include(f => f.Club)
                .FirstOrDefaultAsync(f => f.Id == footballerId);

            if (footballer == null)
                throw new Exception("Không tìm thấy cầu thủ.");

            if (footballer.ClubId != clubId)
                throw new Exception("Cầu thủ này không thuộc CLB của bạn.");

            if (additionalYears <= 0)
                throw new Exception("Số năm gia hạn không hợp lệ.");

            if (additionalYears + footballer.ContractYears > 5)
                throw new Exception("Hợp đồng không thể vượt quá 5 năm.");

            if (footballer.ContractYears <= 0)
                throw new Exception("Cầu thủ này không có hợp đồng để gia hạn.");

            if(footballer.ContractYears > 1)
                throw new Exception("Chỉ có thể gia hạn hợp đồng khi còn 1 năm.");

            var fee = TransferHelper.CalculateTransferPrice(footballer);

            var renewOffer = new Transfer
            {
                FootballerId = footballerId,
                FromClubId = clubId,
                ToClubId = clubId,
                TransferFee = fee, 
                ContractYears = additionalYears,
                Status = "Pending"
            };

            await _context.Transfers.AddAsync(renewOffer);
            await _context.SaveChangesAsync();
        }

        // Gửi lời đề nghị chuyển nhượng cầu thủ đang thuộc CLB khác
        public async Task AddTransferOfferAsync(int fromClubId, int footballerId, int toClubId, int contractYears)
        {
            var footballer = await _context.Footballers
                .Include(f => f.Club)
                .FirstOrDefaultAsync(f => f.Id == footballerId);

            if (footballer == null)
                throw new Exception("Không tìm thấy cầu thủ.");

            if (!footballer.IsTransferListed)
                throw new Exception("Cầu thủ này không có trên thị trường chuyển nhượng.");

            if (footballer.ClubId == null)
                throw new Exception("Cầu thủ này đang thất nghiệp. Dùng API FreeAgentOffer.");

            if (footballer.ClubId == toClubId)
                throw new Exception("Cầu thủ này đã thuộc CLB của bạn.");

            var fee = TransferHelper.CalculateTransferPrice(footballer);
            var transfer = new Transfer
            {
                FootballerId = footballerId,
                FromClubId = footballer.ClubId,
                ToClubId = toClubId,
                ContractYears = contractYears,
                TransferFee = fee,
                Status = "Pending"
            };

            await _context.Transfers.AddAsync(transfer);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasRenewOfferAsync(int footballerId, int clubId)
        {
            return await _context.Transfers
                .AnyAsync(t => t.FootballerId == footballerId && t.FromClubId == clubId && t.ToClubId == clubId && t.Status == "Pending");
        }

        // Trong TransferRepository
        public async Task AcceptTransferAsync(Transfer transfer, Footballer footballer)
        {
            transfer.Status = "Accepted";
            // Nếu là renew (FromClubId == ToClubId) -> cộng thêm số năm
            if (transfer.FromClubId.HasValue && transfer.FromClubId == transfer.ToClubId)
            {
                // gia hạn hợp đồng: cộng thêm số năm
                footballer.ContractYears = Math.Min(5, footballer.ContractYears + transfer.ContractYears);
            }
            else
            {
                // transfer thực sự: đặt số năm hợp đồng theo offer
                footballer.ContractYears = transfer.ContractYears;
            }

            footballer.ClubId = transfer.ToClubId;
            footballer.IsTransferListed = false;

            // Chuyển tiền: trừ tiền bên mua (đã làm ở caller), cộng tiền bên bán nếu có
            if (transfer.FromClubId.HasValue && transfer.TransferFee > 0)
            {
                var sellingClub = await _context.Clubs.FindAsync(transfer.FromClubId.Value);
                if (sellingClub != null)
                {
                    sellingClub.Money += transfer.TransferFee;
                    _context.Clubs.Update(sellingClub);
                }
            }

            _context.Transfers.Update(transfer);
            _context.Footballers.Update(footballer);
            await _context.SaveChangesAsync();
        }

        public async Task RejectTransferAsync(Transfer transfer)
        {
            transfer.Status = "Rejected";
            _context.Transfers.Update(transfer);
            await _context.SaveChangesAsync();
        }

        // Lấy danh sách các đề nghị chuyển nhượng của 1 cầu thủ cụ thể thuộc CLB đang chơi
        public async Task<List<Transfer>> GetPendingTransfersForClubAsync(int clubId, int footballerId)
        {
            return await _context.Transfers
                .Include(t => t.Footballer)
                .Where(t => t.FromClubId == clubId && t.Status == "Pending" && t.FootballerId == footballerId)
                .ToListAsync();
        }

        
    }
}
