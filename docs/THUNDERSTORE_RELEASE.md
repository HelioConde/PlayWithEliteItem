# Preparação do pacote Thunderstore

A versão pública `CONDE/PlayWithEliteItem/1.0.1` ainda contém o pacote de dependências R2API e mecânica antiga de troca. A versão do GitHub `1.2.0` é um **candidato ainda não validado por completo no jogo**.

## Manifesto enxuto

`thunderstore/manifest.json`:

```json
"dependencies": ["bbepis-BepInExPack-5.4.2122"]
```

A partir de 1.2.0, PlayWithEliteItem não usa hooks de equipamentos `On.RoR2`; logo, não exige HookGenPatcher diretamente. BepInExPack pode trazer outras dependências transitivas; não as duplique no manifesto.

## Gerar pacote candidato

```bash
dotnet build GiveEliteItem.sln -c Release
python scripts/validate_manifest.py
python scripts/package_thunderstore.py
```

O packager utiliza o ícone original de `thunderstore/icon.png` ou o recupera do pacote público do próprio autor. O CI gera a DLL e o ZIP candidato, mas **não faz upload no Thunderstore**.

## Verificações antes da publicação

- Confirme que o elite fica fixo no slot 1 enquanto os equipamentos comuns são coletados e usados no Q.
- Teste todos os efeitos passivos, além da aparência visual.
- Teste troca de fase, morte/respawn, entrada tardia e multiplayer com cliente sem o mod.
- Valide o manifesto no validador oficial do Thunderstore.
- Publique com a equipe existente `CONDE`, somente após o teste completo.
