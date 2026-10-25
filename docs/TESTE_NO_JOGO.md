# PlayWithEliteItem 1.1.1 — roteiro de teste

Use a **DLL do GitHub Actions**, não a versão 1.0.1 publicada no Thunderstore.

## Preparação

1. Crie um perfil isolado no r2modman, com BepInExPack e somente o `GiveEliteItem.dll` atualizado.
2. Confirme que **não há duas versões** de `GiveEliteItem.dll` instaladas.
3. Abra `BepInEx/config/AlchemyFlame.PlayWithEliteItem.cfg` e mantenha as opções padrão no primeiro teste.
4. Se precisar investigar algum erro, altere `Diagnostics.VerboseLogging` para `true` e reinicie.

## Verificações no jogo

- [ ] **Solo:** um jogador recebe um equipamento de elite no slot ativo, com o efeito passivo esperado.
- [ ] **Fase seguinte:** o equipamento não é concedido novamente e não é sobrescrito.
- [ ] **Substituição:** ao pegar outro equipamento, o comportamento segue a mecânica normal do jogo; o bônus elite não fica permanentemente aplicado.
- [ ] **Outra partida:** após voltar ao menu e iniciar uma run nova, o sorteio volta a acontecer.
- [ ] **Multiplayer:** coloque a DLL **somente no host**, entre com um cliente sem o mod e confira a sincronização. *A compatibilidade ainda não foi confirmada.*
- [ ] **Entrada tardia:** um cliente que entra durante a partida recebe um aspecto quando o inventário fica pronto.
- [ ] **DLC desativada:** nenhum aspecto exclusivo de uma DLC desabilitada pode ser sorteado.
- [ ] **Equipamento ocupado:** com `ReplaceExistingEquipment=false`, o equipamento existente não é substituído.
- [ ] **Muitos jogadores:** quando o número de jogadores ultrapassa o de aspectos disponíveis, há repetições, sem travamentos.

## Como relatar um problema

Informe: versão do jogo; versão do BepInExPack; solo ou multiplayer; personagem; lista de mods do perfil; configurações alteradas; o que deveria acontecer; o que aconteceu.

Anexe o trecho relevante do `BepInEx/LogOutput.log` (remova dados pessoais antes de compartilhar).

**Status de QA:** compilação e testes automatizados não substituem nenhuma das verificações acima. Não publicar no Thunderstore até concluir o teste real.
