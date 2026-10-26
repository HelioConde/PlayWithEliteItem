# PlayWithEliteItem 1.1.7 — teste no Risk of Rain 2

A versão **1.1.2** armazenava o elite numa reserva invisível ao nascer. A **1.1.7** deve mostrar o elite no slot Q desde o início.

## Preparação

1. Use um perfil com **BepInExPack e HookGenPatcher 1.2.9**, além de somente um `GiveEliteItem.dll`.
2. Baixe a DLL do GitHub Actions da versão **1.1.7**.
3. Inicie pelo gerenciador e confirme **`[PWEI][BOOT]`** no `BepInEx/LogOutput.log`. O mod aparecer na aba General, sozinho, não confirma que o `Awake()` foi executado.
4. O novo `Diagnostics.TraceGameplay=true` já ativa os logs de investigação por padrão; mantenha-o ligado durante o teste.

## Teste principal

- [ ] Entre numa partida solo com Mercenário ou outro personagem de um slot de equipamento.
- [ ] **Imediatamente ao nascer, o elite aparece no Q e o buff/efeito de elite é aplicado.**
- [ ] O log contém `[PWEI][GRANT] SUCCESS ...`
- [ ] Pegue o Radar Escaneador (ou outro equipamento). Ele deve substituir o elite no Q.
- [ ] **O elite não deve cair no chão:** deve estar protegido na segunda reserva lógica.
- [ ] Use o Radar. Após 0,3 segundo, o elite deve voltar ao Q, e o Radar ficará guardado.
- [ ] Verifique que não existem duplicações ou perdas de equipamentos ao coletar outros itens.
- [ ] Confirme recargas, mudança de fase, respawn, multiplayer com cliente vanilla e entrada tardia.

**Limitação atual:** elites em geral não têm ativação; após trocar do Radar para o elite, uma segunda ativação para voltar ao Radar pode não ser possível. Vamos tratar este caso separadamente.

Se o Q continuar vazio após o spawn, envie o log após iniciar a partida. Procure `[PWEI][CATALOG] No eligible elite aspects` ou `Failed to equip starting elite`; isso ajudará a diferenciar catálogo e inventário de um problema de carregamento.

## Diagnóstico para Q vazio

O mod aparecer na aba General do BepInEx confirma **apenas o carregamento**. Se o Q continuar vazio, use a aba **Console** e procure:

- `No eligible elite aspects`: a lista de elites ficou vazia.
- `Waiting for character body`: ainda está aguardando o personagem nascer. 
- `[PWEI][GRANT] FAILED`: a tentativa de colocar o equipamento no slot ativo falhou.
- `not the network server`: o mod não está rodando no host.
- `[PWEI][GRANT] SUCCESS`: o servidor concedeu o aspecto; se o Q continuar vazio, investigaremos a sincronização com o personagem/UI original.
- `Run is active, but no PlayerCharacterMasterController`: ainda não foi encontrado um controlador de jogador.

**Pegar um equipamento normal e vê-lo no Q não confirma o funcionamento do mod.** O teste começa verificando o elite ao nascer, *antes* de coletar qualquer item.

Salve e envie o `LogOutput.log` da **mesma partida** para determinar a causa com precisão.

## Diagnóstico novo da versão 1.1.7

Abra o **BepInEx/LogOutput.log** após entrar numa partida nova. Pesquise `[PWEI]` para ver o fluxo do mod:

| Prefixo | O que significa |
| --- | --- |
| `[PWEI][BOOT]` | Plugin iniciado e valores das configurações |
| `[PWEI][RUN]` | Partida detectada, servidor ativo |
| `[PWEI][CATALOG]` | Cada elite permitido: nome, índice e disponibilidade |
| `[PWEI][SCAN]` | Total de controladores e possíveis motivos para não reconhecer jogador |
| `[PWEI][GRANT]` | Tentativa e resultado da concessão ao inventário |
| `[PWEI][VERIFY]` | Checagem 1,5 segundo após a concessão; detecta equipamento sobrescrito |
| `[PWEI][PICKUP]` | Recebimento de outro equipamento e destino do elite |
| `[PWEI][USE]` / `[PWEI][SWAP]` | Ativação e troca entre equipamento e reserva |

As linhas de `[SCAN]` aparecem no máximo a cada **10 segundos**.
`[CATALOG]` é registrado na inicialização da partida e quando o catálogo passa a ficar disponível.
Mensagens de falhas repetidas por jogador são agrupadas.
O novo `Diagnostics.TraceGameplay=true` está habilitado por padrão, independente do `VerboseLogging` antigo.

**Envie o LogOutput.log completo após nascer e aguardar cerca de 3 segundos, sem coletar itens.**
Se preferir não compartilhar nomes de jogadores, remova-os antes de enviar.
