# PlayWithEliteItem 1.1.4 — teste no Risk of Rain 2

A versão **1.1.2** armazenava o elite numa reserva invisível ao nascer. A **1.1.4** deve mostrar o elite no slot Q desde o início.

## Preparação

1. Use um perfil com BepInExPack completo e somente o `GiveEliteItem.dll` mais recente.
2. Baixe a DLL do GitHub Actions da versão **1.1.4**.
3. Inicie pelo gerenciador e confirme `PlayWithEliteItem 1.1.4 loaded` no `BepInEx/LogOutput.log`.
4. Opcional: ajuste `Diagnostics.VerboseLogging=true` e reinicie para investigar trocas.

## Teste principal

- [ ] Entre numa partida solo com Mercenário ou outro personagem de um slot de equipamento.
- [ ] **Imediatamente ao nascer, o elite aparece no Q e o buff/efeito de elite é aplicado.**
- [ ] O log contém `Equipped STARTING elite ... in the active equipment slot.`
- [ ] Pegue o Radar Escaneador (ou outro equipamento). Ele deve substituir o elite no Q.
- [ ] **O elite não deve cair no chão:** deve estar protegido na segunda reserva lógica.
- [ ] Use o Radar. Após 0,3 segundo, o elite deve voltar ao Q, e o Radar ficará guardado.
- [ ] Verifique que não existem duplicações ou perdas de equipamentos ao coletar outros itens.
- [ ] Confirme recargas, mudança de fase, respawn, multiplayer com cliente vanilla e entrada tardia.

**Limitação atual:** elites em geral não têm ativação; após trocar do Radar para o elite, uma segunda ativação para voltar ao Radar pode não ser possível. Vamos tratar este caso separadamente.

Se o Q continuar vazio após o spawn, envie o log após iniciar a partida. Procure `No eligible elite aspects` ou `Failed to equip starting elite`; isso ajudará a diferenciar catálogo e inventário de um problema de carregamento.

## Diagnóstico para Q vazio

O mod aparecer na aba General do BepInEx confirma **apenas o carregamento**. Se o Q continuar vazio, use a aba **Console** e procure:

- `No eligible elite aspects`: a lista de elites ficou vazia.
- `Skipping elite assignment`: o inventário tem uma estrutura de slots/conjuntos não aceita pelo mod.
- `Equipped STARTING elite`: o servidor concedeu o aspecto; se o Q continuar vazio, investigaremos a sincronização com o personagem/UI original.
- `Run is active, but no PlayerCharacterMasterController`: ainda não foi encontrado um controlador de jogador.

**Pegar um equipamento normal e vê-lo no Q não confirma o funcionamento do mod.** O teste começa verificando o elite ao nascer, *antes* de coletar qualquer item.

Salve e envie o `LogOutput.log` da **mesma partida** para determinar a causa com precisão.
