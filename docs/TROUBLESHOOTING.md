# PlayWithEliteItem — erro MMHOOK_RoR2 identificado no log

## Por que o Q estava vazio?

O `LogOutput(2).log` da versão 1.1.6 mostra:

```text
[Info   : BepInEx] Loading [PlayWithEliteItem 1.1.6]
[Error  : Unity Log] FileNotFoundException:
Could not load file or assembly 'MMHOOK_RoR2, Version=0.0.0.0'
```

O BepInEx **encontrou** o plugin, mas o jogo não conseguiu executar o
`Awake()` porque `MMHOOK_RoR2.dll` não estava disponível. Isso explica
a ausência completa de mensagens `[PWEI]` e a falta do elite ao nascer.

O mesmo log lista quatro patchers: BepInEx.Preloader, SeekersPatcher,
BepInEx.GUI.Loader e FixPluginTypesSerialization. **HookGenPatcher não está
entre eles**.

## Corrigir seu perfil r2modman

1. Abra o **mesmo perfil** que usa para jogar.
2. Instale **RiskofThunder / HookGenPatcher v1.2.9**, com todas as dependências.
3. Inicie com **Start modded** para gerar `MMHOOK_RoR2.dll` no preloader.
4. Confira o `LogOutput.log`: o patcher deve aparecer no início, seguido de
   `[PWEI][BOOT]`. Ao iniciar uma partida, devem aparecer `[PWEI][RUN]`,
   `[PWEI][CATALOG]` e `[PWEI][GRANT]`.
5. Confira a DLL normalmente gerada em
   `BepInEx/plugins/MMHOOK/MMHOOK_RoR2.dll` dentro do perfil.

**A versão 1.1.6 pode ser testada sem recompilar**, simplesmente instalando
HookGenPatcher. A versão 1.1.7 documenta a dependência no manifesto.

Se não carregar, envie o novo `LogOutput.log`. Não copie DLLs de sites
desconhecidos nem reinstale a antiga suíte R2API inteira.

## Por que o manifesto da versão anterior estava errado?

O pacote `MMHOOK.RoR2` em `GiveEliteItem.csproj` é uma referência para
**compilar** os hooks `On.RoR2`. No computador do jogador, HookGenPatcher
é quem gera a assembly correspondente antes de carregar os plugins.
Portanto, o manifesto precisa declarar **duas dependências diretas**:

```json
"dependencies": [
  "bbepis-BepInExPack-5.4.2122",
  "RiskofThunder-HookGenPatcher-1.2.9"
]
```

O GitHub Actions verifica o manifesto, mas não executa o jogo.
Apenas o próximo teste real pode comprovar o funcionamento da mecânica.

## Erro anterior de Newtonsoft.Json

Uma instalação anterior apresentou erro distinto, relacionado a
`Newtonsoft.Json 12.0.0.0`. O log atual da versão 1.1.6 fornece
um diagnóstico específico e diferente: ausência de `MMHOOK_RoR2`.
