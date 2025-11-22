using FootballManager.Business.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FootballManagerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransferController : ControllerBase
    {
        private readonly ITransferService _transferService;
        public TransferController(ITransferService transferService)
        {
            _transferService = transferService;
        }

        //Lấy danh sách cầu thủ có thể chuyển nhượng
        [HttpGet("market")]
        public async Task<IActionResult> GetTransferMarket()
        {
            var list = await _transferService.GetListFootballerCanTransferAsync();
            return Ok(list);
        }

        //Lấy danh sách yêu cầu chuyển nhượng đang chờ xử lý cho CLB
        [HttpGet("pending-transfers")]
        public async Task<IActionResult> GetPendingTransfers([FromQuery] int footballerId)
        {
            var list = await _transferService.GetPendingTransfersForClubAsync(footballerId);
            return Ok(list);
        }

        //Gửi lời đề nghị chiêu mộ cầu thủ tự do
        [HttpPost("free-agent-offer")]
        public async Task<IActionResult> SendOfferToFreeAgent([FromQuery] int clubId, [FromQuery] int footballerId, [FromQuery] int contractYears)
        {
            await _transferService.SendOfferToFreeAgentAsync(clubId, footballerId, contractYears);
            return Ok(new { message = "Lời đề nghị đã được gửi." });
        }

        //Gia hạn hợp đồng cho cầu thủ
        [HttpPost("renew-contract")]
        public async Task<IActionResult> RenewContract([FromQuery] int clubId, [FromQuery] int footballerId, [FromQuery] int additionalYears)
        {
            await _transferService.RenewContractAsync(clubId, footballerId, additionalYears);
            return Ok(new { message = "Yêu cầu gia hạn đã được tạo. Cầu thủ sẽ quyết định đồng ý hay không." });
        }

        // --- Gửi yêu cầu chuyển nhượng cầu thủ thuộc CLB khác ---
        [HttpPost("transfer-offer")]
        public async Task<IActionResult> SendTransferOffer([FromQuery] int fromClubId, [FromQuery] int footballerId, [FromQuery] int toClubId, [FromQuery] int contractYears)
        {
            await _transferService.SendTransferOfferAsync(fromClubId, footballerId, toClubId, contractYears);
            return Ok(new { message = "Lời đề nghị chuyển nhượng đã được gửi." });
        }

        //Giai đoạn BOT CLB gửi đề nghị tự động
        [HttpPost("bot-send-offers")]
        public async Task<IActionResult> BotSendOffersAsync()
        {
            await _transferService.BotDecideWhoToBuyOrRenewAsync();
            return Ok(new { message = "Bot CLB đã gửi đề nghị xong." });
        }

        //Cầu thủ chọn CLB (xử lý tất cả pending transfer)
        [HttpPost("player-and-bot-decide")]
        public async Task<IActionResult> ResolvePendingTransfersAsync()
        {
            await _transferService.BotDecideTransfersAsync();
            return Ok(new { message = "Các cầu thủ đã chọn CLB xong." });
        }

    }
}
