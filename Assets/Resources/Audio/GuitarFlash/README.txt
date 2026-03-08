========================================
  GUITAR FLASH — Pasta de Músicas
========================================

COMO ADICIONAR UMA MÚSICA:
1. Coloque o arquivo de áudio (.mp3 ou .ogg) diretamente nesta pasta
2. (Opcional) Crie um arquivo de texto com o mesmo nome + _info.txt

EXEMPLO:
  Assets/Resources/Audio/GuitarFlash/
    insonia.ogg
    insonia_info.txt    ← conteúdo: "Insônia|Hungria ft Tribo da Periferia"
    rock-balboa.mp3
    rock-balboa_info.txt ← conteúdo: "Rock Balboa|Artista"

FORMATO DO info.txt:
  Título da Música|Nome do Artista

NOTAS:
- O jogo detecta automaticamente todas as músicas nesta pasta
- As batidas são analisadas por FFT (análise de frequência) automaticamente
- Formatos aceitos: .mp3, .ogg, .wav
- Se não houver info.txt, o nome do arquivo será usado como título
