using ApiCrud.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiCrud.Estudantes
{
    public static class EstudantesRoutes
    {
        public static void RotasEstudantes (this WebApplication app)
        {
            var GrupoEstudante = app.MapGroup("Estudantes");

            //Cancellation Token "avisa" o banco para parar de rodar e evita travar as tabelas.
            GrupoEstudante.MapPost(pattern: "", handler:async (AddEstudanteRequest request, ApiDbContext context, CancellationToken ct) =>
            {
                var jaExiste = await context.Estudantes.AnyAsync(estudante => estudante.Nome == request.Nome, ct);

                if (jaExiste)
                    return Results.Conflict(error:"Estudante já cadastrado");



                var novoEstudante = new Estudante(request.Nome);
                await context.Estudantes.AddAsync(novoEstudante, ct);
                await context.SaveChangesAsync(ct);

                var estudanteRetorno = new EstudanteDto(novoEstudante.Id, novoEstudante.Nome);

                return Results.Ok(estudanteRetorno);
            });

            GrupoEstudante.MapGet(pattern: "", handler: async (ApiDbContext context,CancellationToken ct) =>
            {
                var estudantes = await context.Estudantes.Where(estudantes => estudantes.Ativo)
                .Select(estudantes => new EstudanteDto(estudantes.Id, estudantes.Nome))
                .ToListAsync(ct);
                return estudantes;
            });

            GrupoEstudante.MapPut(pattern: "{id:guid}", handler: async(Guid id, UpdateEstudanteRequest request, ApiDbContext context, CancellationToken ct) =>
            {
                var estudante = await context.Estudantes.SingleOrDefaultAsync(estudante=>estudante.Id == id, ct);

                if (estudante == null)
                    return Results.NotFound();

                estudante.AtualizarNome(request.Nome);

                //Após o save changes ser executado, é feito "automaticamente" um update
                await context.SaveChangesAsync(ct);
                return Results.Ok(new EstudanteDto(estudante.Id, estudante.Nome));


            });

            //Soft Delete
            GrupoEstudante.MapDelete(pattern: "{id}", handler: async (Guid id, ApiDbContext context, CancellationToken ct) =>
            {
                var estudante = await context.Estudantes.SingleOrDefaultAsync(estudante => estudante.Id == id, ct);

                if (estudante == null)
                    return Results.NotFound();

                estudante.Desativar();

                await context.SaveChangesAsync(ct);
                return Results.Ok();
            });

        }
    }
}
