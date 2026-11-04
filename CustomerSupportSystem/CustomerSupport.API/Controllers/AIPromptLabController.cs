using CustomerSupport.API.Models;
using CustomerSupport.Application.DTOs.AI;
using CustomerSupport.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers
{
    [ApiController]
    [Route("api/ai/prompt-lab")]
    [Authorize(Roles = "Administrator,SupportExecutive")]
    public sealed class AIPromptLabController : ControllerBase
    {
        private readonly IPromptExperimentService _service;

        public AIPromptLabController(IPromptExperimentService service)
        {
            _service = service;
        }

        // POST /api/ai/prompt-lab/experiment
        [HttpPost("experiment")]
        public async Task<ActionResult<ApiResponse<PromptExperimentResponseDTO>>> Experiment(
                [FromBody] PromptExperimentRequestDTO request,
                CancellationToken cancellationToken)
        {
            PromptExperimentResponseDTO result =
                await _service.TestAsync(request, cancellationToken);

            return Ok(ApiResponse<PromptExperimentResponseDTO>
                .SuccessResponse(result, "Prompt experiment completed."));
        }
    }
}