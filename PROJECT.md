# Will Apps Unity

Migração do aplicativo **will-apps-pwa** (React/TypeScript) para uma aplicação nativa Android usando **Unity + C#**.

## Sobre o Projeto

O objetivo é portar os jogos e funcionalidades do app PWA para Unity, ganhando melhor performance, suporte a jogos mais complexos e maior compatibilidade com dispositivos Android.

**Repositório:** `git@github.com:ehurafa/will-apps-unity.git`

> ⚠️ **IMPORTANTE: Abordagem Code-Only (Vibe Coding)**
>
> O desenvolvedor deste projeto **não tem experiência com o Unity Editor**.
> Portanto, toda a criação de GameObjects, UI, física e lógica deve ser feita
> **100% via código C#**, utilizando scripts "Setup" que constroem tudo
> programaticamente em runtime.
>
> **O uso do Unity Editor deve ser o mínimo possível:**
> - Criar cenas vazias
> - Anexar UM ÚNICO script Setup na Main Camera de cada cena
> - Registrar as cenas no Build Profiles
>
> **Nunca peça para o usuário arrastar componentes, configurar o Inspector
> manualmente, ou criar GameObjects pela Hierarchy.**

---

## Regras de Negócio

### Público-Alvo
- **Usuário principal:** Criança de 12 anos.
- O app deve ser **simples, intuitivo e bonito** (com cara de jogo).
- Roda em um **único aparelho Android**.

### Conceito do App
O app é um hub de entretenimento pessoal com duas grandes seções:

1. **Assistir** 🎬 — Streaming de vídeos (Animes e Filmes).
2. **Jogar** 🎮 — Coleção de mini-jogos casuais.

### Fluxo de Navegação

```
MainMenu (Home)
├── Assistir (Watch)
│   ├── Tab: Animes
│   └── Tab: Filmes
│   └── VideoPlayer (ao selecionar um vídeo)
└── Jogar (Play)
    ├── Flappy Bird
    ├── Jogo da Velha (vs IA)
    ├── Flappy Ninja (tema Naruto)
    ├── Sinuca (futuro)
    └── UNO (futuro)
```

### Seção "Assistir" (Watch)
- Organizada em categorias via **tabs**: `Animes` e `Filmes`.
- Os vídeos são listados em um grid com **thumbnail**, **título** e **duração**.
- Ao clicar em um vídeo, abre o player em tela cheia.
- **Origem dos vídeos:** Hospedados na Hostinger (hospedagem tradicional), consumidos via URL direta (MP4).
- Funcionalidade de **transmitir para TV** (cast) é desejável.

### Seção "Jogar" (Play)
- Lista de jogos em cards com ícone, nome e descrição.
- Jogos indisponíveis aparecem como **"Bloqueado"**.
- Todos os jogos são **single-player** (1 jogador contra a máquina).

### Regras dos Jogos

#### Flappy Bird
- O jogador controla um pássaro e deve desviar de canos.
- **Toque na tela** faz o pássaro pular.
- Canos surgem da direita com **gap aleatório** entre o cano superior e inferior.
- **Pontuação:** +1 ponto ao passar por cada par de canos.
- **Game Over:** Colidir com cano ou sair dos limites da tela (topo/fundo).
- **High Score:** Salvo localmente no dispositivo.

#### Jogo da Velha (Tic-Tac-Toe)
- Jogador é sempre **X**, IA é sempre **O**.
- Jogador começa primeiro.
- **IA com estratégia:** Tenta vencer → Bloqueia jogador → Centro → Cantos → Qualquer espaço.
- **Delay da IA:** 500ms antes de jogar (para parecer que "pensa").
- **Placar persistente** durante a sessão (Jogador vs IA).
- Botão de **resetar placar** e **jogar novamente**.
- Detecta empate quando todas as células estão preenchidas.

#### Flappy Ninja (tema Naruto) — Futuro
- Variação do Flappy Bird com tema do anime Naruto.
- Especificações desejadas:
  - Escolha de personagem: Naruto, Sasuke ou Sakura.
  - Animação ao pular.
  - Sons: fundo (`theme.mp3`), pulo (`jump.mp3`), colisão (`collision.mp3`).
  - Background com leve animação de scale.

#### Sinuca e UNO — Futuro
- Planejados mas ainda não iniciados.
- Serão single-player contra IA.

### Hospedagem de Assets
- Todos os assets e mídias que precisem de hospedagem serão hospedados na **Hostinger** (serviço de hospedagem tradicional).
- Upload via **FTP**.
- Consumo via URL direta no app.

### Design e Identidade Visual

#### Paleta de Cores (do app original)
| Cor                | Hex       | Uso                          |
|--------------------|-----------|------------------------------|
| Primary            | `#FF6B6B` | Vermelho vibrante            |
| Secondary          | `#4ECDC4` | Azul-turquesa                |
| Accent             | `#FFE66D` | Amarelo (destaque, bird)     |
| Success            | `#95E1D3` | Verde água (pipes)           |
| Background         | `#1A1A2E` | Azul escuro (fundo geral)    |
| Card               | `#16213E` | Azul card                    |
| Text               | `#FFFFFF` | Texto principal              |
| Text Secondary     | `#A8DADC` | Texto secundário             |

#### Gradientes
- **Primary:** `#FF6B6B` → `#FF8E53` (botão Assistir)
- **Secondary:** `#4ECDC4` → `#44A08D` (botão Jogar)
- **Accent:** `#FFE66D` → `#FFAB4C` (cards de jogos)

### Convenções de Código
- **Nomes de componentes, funções e variáveis:** Inglês.
- **Textos de UI (labels, mensagens):** Português (pt-BR).
- **Naming no Unity:** PascalCase para classes/scripts, camelCase para variáveis.
- **GameObjects no Editor:** Prefixo `Btn_` para botões.

---

## Stack

| Tecnologia       | Versão / Detalhe          |
|------------------|---------------------------|
| Unity            | 6 LTS (6000.0.x)         |
| Linguagem        | C#                        |
| Template         | Universal 2D (URP)        |
| Plataforma Alvo  | Android                   |
| IDE              | Visual Studio Community 2022 |

---

## Estrutura do Projeto

```
Assets/
├── Scenes/
│   ├── MainMenu.unity         ✅ Testada
│   ├── Play.unity             ✅ Configurada
│   ├── Watch.unity            ✅ Configurada
│   ├── VideoPlayer.unity      ✅ Configurada
│   ├── FlappyBird.unity       ✅ Configurada
│   └── TicTacToe.unity        ✅ Configurada
├── Scripts/
│   ├── MainMenuSetup.cs       ✅ Setup: Menu Principal
│   ├── PlaySetup.cs           ✅ Setup: Seleção de Jogos
│   ├── WatchSetup.cs          ✅ Setup: Listagem de Vídeos
│   ├── VideoPlayerSetup.cs    ✅ Setup: Player de Vídeo
│   ├── FlappyBirdSetup.cs     ✅ Setup: Monta o jogo Flappy Bird
│   ├── BirdController.cs      ✅ Física do pássaro
│   ├── PipeSpawner.cs         ✅ Gera canos
│   ├── PipeMove.cs            ✅ Move canos
│   ├── FlappyGameManager.cs   ✅ Estado do jogo, pontuação
│   └── TicTacToeSetup.cs      ✅ Setup: Jogo da Velha completo
├── Settings/
└── TextMesh Pro/
```

### Como os scripts Setup funcionam
Cada cena precisa apenas da **Main Camera** com o script Setup correspondente.
Ao dar Play, o script cria **todos** os GameObjects, Canvas, UI, física e lógica
automaticamente via código. Nenhuma configuração manual no Editor é necessária.

---

## Funcionalidades

### 1. Main Menu (Cena: `MainMenu`) ✅ Testada

Menu principal com navegação entre as telas do app.

- **Script:** `MainMenuSetup.cs` (anexar na Main Camera)
- **Criado via código:**
  - Canvas com título "Will Apps" e subtítulo
  - Botão ASSISTIR → Carrega cena `Watch`
  - Botão JOGAR → Carrega cena `Play`
  - Footer com versão
- **Canvas Scaler:** Scale With Screen Size (1080x1920)

### 2. Play - Seleção de Jogos (Cena: `Play`) ✅

Telass de seleção de jogos com cards.

- **Script:** `PlaySetup.cs` (anexar na Main Camera)
- **Criado via código:**
  - Header com botão Voltar + título "Jogar"
  - ScrollView com cards de jogos (ícone, nome, descrição)
  - Jogos disponíveis: Flappy Bird, Jogo da Velha → navegam para suas cenas
  - Jogos bloqueados: Flappy Ninja, Sinuca, UNO → badge "Bloqueado"

### 3. Watch - Listagem de Vídeos (Cena: `Watch`) ✅

Tela de listagem de vídeos com categorias.

- **Script:** `WatchSetup.cs` (anexar na Main Camera)
- **Criado via código:**
  - Header com botão Voltar + título "Assistir"
  - Tabs: Animes | Filmes (toggle entre categorias)
  - Cards de vídeo com thumbnail (carregada via URL), título e duração
  - Ao clicar, passa URL do vídeo via campo estático e carrega cena VideoPlayer
  - Dados mock hardcoded (Demon Slayer, Big Buck Bunny, Naruto, One Piece)

### 4. Video Player (Cena: `VideoPlayer`) ✅

Player de vídeo nativo do Unity com streaming via URL.

- **Script:** `VideoPlayerSetup.cs` (anexar na Main Camera)
- **Criado via código:**
  - Video Player (URL, Camera Near Plane)
  - Barra superior: Botão Voltar + Título
  - Barra inferior: Status + Botão Play/Pause
  - URL padrão: Big Buck Bunny (MP4)

### 5. Flappy Bird (Cena: `FlappyBird`) ✅

Jogo Flappy Bird completo com física 2D.

- **Script:** `FlappyBirdSetup.cs` (anexar na Main Camera)
- **Scripts auxiliares:** `BirdController.cs`, `PipeSpawner.cs`, `PipeMove.cs`, `FlappyGameManager.cs`
- **Criado via código:**
  - Pássaro (Circle Sprite amarelo, Rigidbody2D, pulo por toque)
  - Canos com gap aleatório e score trigger
  - Bordas (topo/fundo)
  - UI: Pontuação, Recorde, Tela Inicial, Game Over com Restart
  - High Score salvo via PlayerPrefs

### 6. Jogo da Velha (Cena: `TicTacToe`) ✅

Jogo da Velha completo com IA.

- **Script:** `TicTacToeSetup.cs` (anexar na Main Camera)
- **Criado via código:**
  - Tabuleiro 3x3 (GridLayout)
  - Placar Jogador (X) vs IA (O)
  - IA com estratégia: vencer → bloquear → centro → cantos → aleatório
  - Delay de 500ms na IA
  - Painel de resultado + botões Reiniciar e Menu

---

## Configurações Importantes

### Build Profiles (Android)
- Plataforma já configurada para **Android** via Build Profiles.
- Módulos instalados: Android Build Support, Android SDK & NDK Tools, OpenJDK.

### Canvas Scaler (UI Mobile)
Todas as cenas com UI devem usar:
- **UI Scale Mode:** Scale With Screen Size
- **Reference Resolution:** 1080 x 1920
- **Screen Match Mode:** Match Width Or Height (Match: 0.5)

### Scenes no Build
As seguintes cenas devem estar registradas no Build Profiles:
1. `MainMenu` (index 0 — cena inicial)
2. `Play`
3. `Watch`
4. `VideoPlayer`
5. `FlappyBird`
6. `TicTacToe`

### Tags Customizadas (ProjectSettings/TagManager.asset)
- `Obstacle` — Canos e bordas do Flappy Bird
- `Ground` — Chão
- `ScoreTrigger` — Trigger invisível entre os canos para contar pontos

---

## Referência: App Original (PWA)

O app original está em `c:\PROJECTS\will-apps-pwa` e contém:

| Funcionalidade   | Arquivo Original                          |
|------------------|-------------------------------------------|
| Home / Menu      | `src/pages/Home.tsx`                      |
| Video Player     | `src/pages/VideoPlayer.tsx`               |
| Flappy Bird      | `src/games/FlappyBird.tsx`                |
| Jogo da Velha    | `src/games/JogoDaVelha.tsx`               |
| Cores do tema    | `src/constants/colors.ts`                 |

### Constantes do Flappy Bird (original)
```
GRAVITY = 0.5
JUMP_STRENGTH = -8
PIPE_WIDTH = 60
PIPE_GAP = 150
PIPE_SPEED = 2
BIRD_RADIUS = 15
CANVAS_SIZE = 400x600
BACKGROUND_COLOR = #87CEEB
BIRD_COLOR = #FFE66D
PIPE_COLOR = #95E1D3
```

---

## Como Rodar

1. Abra o **Unity Hub**.
2. Adicione o projeto `c:\PROJECTS\will-apps-unity`.
3. Abra com o Unity 6 LTS.
4. Na pasta `Assets/Scenes`, abra a cena `MainMenu`.
5. Pressione **Play** (▶) para testar no Editor.

### Build Android (APK)
1. **File > Build Profiles** (`Ctrl+Shift+B`).
2. Selecione **Android**.
3. Clique em **Build** e escolha onde salvar o APK.
4. Instale o APK no dispositivo Android.

---

## Changelog

### 2026-03-07 — Telas Play e Watch
- Criada cena `Play` com `PlaySetup.cs` — hub de seleção de jogos com cards.
- Criada cena `Watch` com `WatchSetup.cs` — listagem de vídeos com tabs Animes/Filmes.
- Menu principal atualizado: ASSISTIR → Watch, JOGAR → Play.
- Removidos botões individuais de jogos do MainMenu.
- Corrigido bug de gravidade do Flappy Bird (pássaro caía antes de iniciar).
- Corrigido bug de visibilidade nos ScrollViews (substituído `Mask` por `RectMask2D`).
- VideoPlayer agora recebe URL e título do vídeo selecionado na tela Watch.
- Build Profiles agora com 6 cenas.
- Cenas Play.unity e Watch.unity criadas e registradas no Build Profiles.

### 2026-03-06 — Refatoração Code-Only
- Refatoração completa para abordagem 100% via código.
- Removidos scripts manuais (`MainMenuController.cs`, `VideoPlayerController.cs`).
- Criados 8 scripts Setup que constroem tudo programaticamente.
- Menu principal testado com sucesso no Editor.
- Tags customizadas adicionadas ao TagManager.
- **Pendente:** Testar Flappy Bird, Jogo da Velha e Video Player no Editor e no Android.

### 2026-02-16 — Setup Inicial
- Projeto criado no Unity Hub (Universal 2D, Unity 6 LTS).
- Android Build Support configurado.
- Estrutura de pastas criada (Scripts, Scenes, Prefabs, Sprites, Audio).
- Menu principal e Video Player configurados manualmente no Editor.
- Flappy Bird iniciado (Bird com física).
- Repositório conectado ao GitHub.
