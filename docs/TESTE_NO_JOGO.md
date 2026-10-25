# Teste da versão 1.1.2 — PlayWithEliteItem

**Objetivo:** garantir que o elite **não caia ao pegar um equipamento** e que a rotação original volte a funcionar.

## Preparação

- [ ] Crie um perfil separado no r2modman com BepInExPack e suas dependências.
- [ ] Instale **somente a DLL 1.1.2** do último build verde do GitHub Actions. Retire qualquer cópia anterior.
- [ ] Inicie com **Start modded** e confirme `PlayWithEliteItem 1.1.2 loaded` no `LogOutput.log`.
- [ ] Se estiver investigando um problema, ative `Diagnostics.VerboseLogging=true` no arquivo de configuração e reinicie.

## Teste principal

1. Entre em uma partida solo com um sobrevivente de **um único equipamento** (por exemplo, Mercenário).
2. Antes de coletar um equipamento, o servidor deve registrar `Reserved elite ... in logical slot 2`. O slot Q pode começar vazio — o elite está na reserva lógica.
3. Pegue o Radar Escaneador ou outro equipamento utilizável.
4. **Esperado:** o Radar ocupa o slot Q e o elite **não aparece no chão**.
5. Ative o equipamento.
6. **Esperado:** depois de 0,3 segundo, o elite passa ao slot ativo. O equipamento utilizado deve ficar na reserva do servidor.

## Casos adicionais

- [ ] A partida seguinte recebe um elite diferente ou novo sorteio.
- [ ] Nenhum item é duplicado ao tentar usar equipamentos repetidamente.
- [ ] Com o elite ativo, pegar outro equipamento não o joga no chão; o equipamento normal pode ser substituído pela regra padrão.
- [ ] Recargas e cargas são preservadas após uma troca.
- [ ] Outros jogadores recebem reservas independentes.
- [ ] Cliente sem mod vê o equipamento ativo sincronizado pelo host.
- [ ] Personagens com slots/conjuntos especiais não sofrem alterações indevidas.

**Limitação conhecida:** alguns aspectos de elite não têm ação ativável, portanto a troca automática de volta para o Radar pode não ocorrer. Registre esse resultado, pois a prioridade é preservar a lógica original de alternância.

## Se falhar

Envie `BepInEx/LogOutput.log` após executar esses passos, dizendo: quem estava equipado, o que caiu no chão e em que momento. Não confunda erros antigos de `Newtonsoft.Json` com problemas novos do sistema de coleta.

[Carregamento e bibliotecas faltando](TROUBLESHOOTING.md).
