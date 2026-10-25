# Diagnóstico de inicialização — Newtonsoft.Json 12.0.0.0

> **Ocorrência confirmada em 25/10/2026**, com RoR2 1.5.0#1224,
> BepInEx 5.4.21.0 e `GiveEliteItem.dll` 1.1.1.

## Sintoma observado no LogOutput.log

```text
[Info   : BepInEx] 1 plugins to load
[Error  : BepInEx] Error loading [PlayWithEliteItem 1.1.1] :
Could not load file or assembly 'Newtonsoft.Json, Version=12.0.0.0'
```

O preloader e o chainloader do BepInEx iniciaram, localizaram o
PlayWithEliteItem e **falharam ao carregar uma dependência** antes do
método `Awake` do plugin. Isso explica a ausência do arquivo
`BepInEx/config/AlchemyFlame.PlayWithEliteItem.cfg`.

Uma inspeção do binário compilado mostra que **o plugin não tem uma
referência direta à assembly Newtonsoft.Json**; a referência exigida
pode vir de uma dependência transitiva. O log, isoladamente, **não
identifica qual outra assembly faz essa solicitação**.

O mesmo log informa que só foi carregado o patcher
`BepInEx.Preloader`. Isso sugere uma instalação mínima ou incompleta
dos componentes esperados do BepInExPack moderno do RoR2; não é prova
definitiva, mas justifica testar primeiro com uma instalação limpa.

## Correção recomendada antes de alterar o código

1. Crie **um perfil novo e separado** no r2modman para Risk of Rain 2,
   sem copiar a pasta BepInEx existente.
2. Instale **bbepis / BepInExPack 5.4.2122** pelo gerenciador e aceite
   **todas as dependências** que ele solicitar. Não instale somente
   `BepInEx.dll` ou o framework básico.
3. Inicie o jogo com **Start modded**, sem PlayWithEliteItem, e confirme
   que o log foi criado nesse perfil.
4. Feche o jogo. Coloque **somente**
   `GiveEliteItem.dll` 1.1.1 em
   `BepInEx/plugins/PlayWithEliteItem/GiveEliteItem.dll` desse perfil.
   Não coloque uma segunda cópia em `Risk of Rain 2/Mods`.
5. Inicie novamente com **Start modded**. Verifique o log.

### O que deve aparecer

```text
PlayWithEliteItem 1.1.1 loaded.
```

Se a partida realmente começar com servidor local ativo:

```text
Elite distribution ready for the new run.
Eligible elite aspects: ...
```

Após o primeiro carregamento bem-sucedido, procure:

```text
BepInEx/config/AlchemyFlame.PlayWithEliteItem.cfg
```

Se `Newtonsoft.Json 12.0.0.0` continuar faltando com o BepInExPack
**completo e atualizado**, salve o **novo** `LogOutput.log` e reporte
a lista de assemblies/pacotes instalados. **Não copie bibliotecas
aleatórias da internet, não reinstale os 30 módulos R2API antigos e
não sobregrave assemblies dentro de `Risk of Rain 2_Data/Managed`.**

## O que a compilação automatizada consegue verificar

- C# compila e os testes de lógica passam.
- O arquivo `GiveEliteItem.dll` é gerado.
- O manifesto declara apenas BepInExPack diretamente.

**Não é um teste de carregamento no processo Unity/Mono do jogador.**
O problema de assembly ausente só pode ser considerado resolvido
depois de confirmar o novo log em uma instalação real.
