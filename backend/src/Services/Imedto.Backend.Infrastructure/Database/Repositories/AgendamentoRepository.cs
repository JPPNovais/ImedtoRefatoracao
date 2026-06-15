using Imedto.Backend.Domain.Agendamentos;
using Microsoft.EntityFrameworkCore;

namespace Imedto.Backend.Infrastructure.Database.Repositories;

public class AgendamentoRepository : IAgendamentoRepository
{
    private readonly AppDbContext _db;

    public AgendamentoRepository(AppDbContext db) => _db = db;

    public async Task<Agendamento?> ObterPorIdOuNulo(long id, long estabelecimentoId)
        => await _db.Agendamentos
            .FirstOrDefaultAsync(a => a.Id == id && a.EstabelecimentoId == estabelecimentoId);

    /// <inheritdoc/>
    public async Task<Agendamento?> ObterPorTokenOuNulo(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        return await _db.Agendamentos
            .FirstOrDefaultAsync(a => a.TokenConfirmacao == token);
    }

    public async Task Salvar(Agendamento agendamento)
    {
        if (agendamento.Id == 0)
            _db.Agendamentos.Add(agendamento);
        else
            _db.Agendamentos.Update(agendamento);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc/>
    public async Task SalvarAcessoLog(AgendamentoConfirmacaoAcessoLog log)
    {
        await _db.AgendamentoConfirmacaoAcessoLogs.AddAsync(log);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> ExisteConflito(
        long estabelecimentoId,
        Guid profissionalUsuarioId,
        DateTime inicioPrevisto,
        DateTime fimPrevisto,
        long? excluirAgendamentoId = null)
    {
        return await _db.Agendamentos
            .Where(a =>
                a.EstabelecimentoId == estabelecimentoId &&
                a.ProfissionalUsuarioId == profissionalUsuarioId &&
                a.Status != AgendamentoStatus.Cancelado &&
                (excluirAgendamentoId == null || a.Id != excluirAgendamentoId) &&
                a.InicioPrevisto < fimPrevisto &&
                a.FimPrevisto > inicioPrevisto)
            .AnyAsync();
    }

    public async Task<Agendamento?> ObterPorChaveDeNegocioOuNulo(long pacienteId, Guid profissionalUsuarioId, DateTime inicioPrevisto, long estabelecimentoId)
        => await _db.Agendamentos
            .FirstOrDefaultAsync(a =>
                a.PacienteId == pacienteId &&
                a.ProfissionalUsuarioId == profissionalUsuarioId &&
                a.InicioPrevisto == inicioPrevisto &&
                a.EstabelecimentoId == estabelecimentoId);

    public async Task Remover(Agendamento agendamento)
    {
        _db.Agendamentos.Remove(agendamento);
        await _db.SaveChangesAsync();
    }
}
