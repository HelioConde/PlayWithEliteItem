# PlayWithEliteItem — solução de problemas

## Como confirmar o carregamento

A presença do nome do mod no BepInExGUI não garante que seu código tenha iniciado. Procure no `LogOutput.log`:

```text
[PWEI][BOOT] version=1.2.0
[PWEI][RUN] New run detected
[PWEI][GRANT] SUCCESS ... slot1=...
[PWEI][VERIFY] ... slot1=... Q=...
```

Se não existir `[PWEI][BOOT]`, confirme que a DLL está na pasta do perfil usado para iniciar o jogo e que o BepInExPack está instalado.

## E se o elite não aparecer?

Procure `[PWEI][CATALOG]` (lista de elites), `[PWEI][SLOT]` (segundo slot ocupado ou ativo), ou `[PWEI][GRANT]` (resultado da concessão). O slot Q pode começar vazio — **o elite fica à direita do Q**.

Se um equipamento já existir no segundo slot, o plugin não o sobrescreve.

## Preciso do HookGenPatcher?

**A partir de 1.2.0, este plugin não usa hooks `On.RoR2` e não requer HookGenPatcher diretamente.** Outros mods instalados no perfil podem precisar dele.

As versões 1.1.6 até 1.1.9 utilizavam hooks e podiam falhar com a ausência de `MMHOOK_RoR2.dll`. A 1.2.0 elimina esses hooks, em vez de criar novas dependências.

## Logs após coleta

O `[PWEI][VERIFY]` reporta periodicamente o conteúdo do slot 1 e do Q e indica se o buff de elite foi detectado. **Esta versão não aplica buffs manualmente nem troca os equipamentos.**
