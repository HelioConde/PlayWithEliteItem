# Roteiro de teste — PlayWithEliteItem 1.2.0

**Objetivo:** conferir que o elite permanece no slot físico 1 e que o Q é totalmente livre, sem alternância automática.

## Antes de começar

- Instale a DLL 1.2.0 no perfil modded, mantendo somente uma versão do plugin.
- Use BepInExPack completo e inicie por **Start modded**.
- Confirme no `BepInEx/LogOutput.log`: `[PWEI][BOOT] version=1.2.0`.

## Testes

- [ ] Ao nascer, o elite aparece **à direita do Q**, não dentro do Q.
- [ ] O efeito visual do elite aparece antes de coletar equipamentos comuns.
- [ ] No log: `[PWEI][GRANT] SUCCESS ... slot1=...`.
- [ ] Pegue um Radar ou Tricórnio; o equipamento normal fica no Q, elite permanece à direita.
- [ ] Use o equipamento do Q: **nenhuma troca automática** ocorre.
- [ ] Pegue outro equipamento: o Q segue a regra normal, e o elite permanece no slot 1.
- [ ] Confirme os efeitos **reais** de elite (não só a aparência visual) contra inimigos.
- [ ] Após mudar de estágio ou renascer, o elite continua equipado.
- [ ] Cliente sem o mod: consegue entrar no host com mod e vê o estado corretamente.
- [ ] Jogadores adicionais recebem elites próprios; nenhum jogador tem equipamentos duplicados.

## Interpretando logs

- `[PWEI][GRANT] SUCCESS`: o servidor concedeu o elite ao slot físico 1.
- `[PWEI][VERIFY] ... slot1=... Q=... vanillaEliteBuffPresent=True`: há buff detectado, sem injeção manual.
- `[PWEI][SLOT] Skipping`: slot físico 1 já estava ocupado/ativo.
- `[PWEI][GRANT] Verification failed`: jogo rejeitou a atribuição ao segundo slot.

O mod não intercepta coleta ou uso de equipamentos. Se o elite sumir ao trocar itens, envie o log e descreva qual equipamento foi coletado e qual estava no Q.

**Nenhuma nova versão foi publicada automaticamente no Thunderstore.**
