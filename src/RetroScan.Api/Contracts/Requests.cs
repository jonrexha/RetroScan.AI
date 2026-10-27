using System.ComponentModel.DataAnnotations;
using RetroScan.Api.Data;

namespace RetroScan.Api.Contracts;

public sealed record CreateSessionRequest([StringLength(ResearchSession.TitleMaxLength)] string? Title);

public sealed record AskRequest([Required, StringLength(Question.TextMaxLength)] string Text);
