
## Обзор

## Предварительные требования

- **Docker и Docker Compose**: Убедитесь, что оба [установлены](https://www.docker.com/) и работают
- **.NET SDK**: Требуется для разработки без Docker
- **PostgreSQL**: Используется в качестве базы данных

## Настройка и установка

### Клонирование репозитория

```bash
git clone https://github.com/KirillTotkov/DelphicGames.git
cd DelphicGames
```

### Конфигурация

Приложение использует файл `appsettings.json` и переменные окружения для конфигурации.

Обновите `appsettings.json` с вашими настройками:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db;Port=5432;Database=delphic_games;Username=user;Password=123456"
  },
  "RootUser": {
    "Email": "root@example.com",
    "Password": "your_root_password"
  },
  "EmailSettings": {
    "From": "no-reply@example.com",
    "SmtpServer": "smtp.example.com",
    "Port": 587,
    "Username": "smtp_user",
    "Password": "smtp_password",
    "EnableSendConfirmationEmail": false
  }
}
```

### Переменные окружения

В качестве альтернативы, вы можете использовать переменные окружения для переопределения настроек:

- `ASPNETCORE_ENVIRONMENT`
- `ConnectionStrings__DefaultConnection`
- `RootUser__Email`
- `RootUser__Password`
- `EmailSettings__...`

При запуске в Docker Compose переменные окружения могут быть определены в файле `.env`.

**Создание файла `.env`:**

Создайте файл `.env` в корне проекта со следующим содержимым:

```env
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__DefaultConnection=Host=db;Port=5433;Database=delphic_games;Username=user;Password=123456
RootUser__Email=root@example.com
RootUser__Password=your_root_password
EmailSettings__From=no-reply@example.com
EmailSettings__SmtpServer=smtp.example.com
EmailSettings__Port=587
EmailSettings__Username=smtp_user
EmailSettings__Password=smtp_password
EmailSettings__EnableSendConfirmationEmail=false
```

## Настройка для разработки

### Использование Docker Compose

1. **Сборка и запуск контейнеров**

   ```bash
   docker-compose up --build
   ```

2. **Доступ к приложению**

    - URL приложения: `http://localhost:8080`
    - Swagger UI: `http://localhost:8080/swagger`

3. **База данных**

    - База данных PostgreSQL доступна на порту `5433`.

### Без Docker

1. **Установка зависимостей**

   Убедитесь, что .NET SDK 8.0 установлен.

2. **Настройка базы данных**

    - Установите PostgreSQL локально или запустите его через Docker:

      ```bash
      docker run -p 5432:5432 -e POSTGRES_USER=user -e POSTGRES_PASSWORD=123456 -e POSTGRES_DB=delphic_games postgres:latest
      ```

    - Обновите `ConnectionStrings:DefaultConnection` в `appsettings.json` соответственно.

3. **Запуск приложения**

   ```bash
   cd DelphicGames
   dotnet run
   ```

4. **Доступ к приложению**

    - URL приложения: `https://localhost:5001` или `http://localhost:5000`

## Подробности конфигурации

### Сервисы Docker Compose

- **db**: Сервис базы данных PostgreSQL
- **delphicgames**: Сервис приложения

### Порты

- **Приложение**: `8080` (HTTP), `8081` (дополнительный сервис, если есть)
- **База данных**: Порт хоста `5433` сопоставлен с портом контейнера `5432`

### Переменные окружения в Docker Compose

Обновите раздел `environment` для сервиса 

delphicgames

 в 

docker-compose.yml

 по мере необходимости.

## Первоначальная настройка

- **Учетная запись супер администратора**

  При первом запуске приложение создает учетную запись root, используя учетные данные из настроек `RootUser`.

## Дополнительные заметки

- **Миграции базы данных**

  Убедитесь, что миграции базы данных применены:

  ```bash
  dotnet ef database update
  ```

- **Подтверждение по электронной почте**

  Установите `EnableSendConfirmationEmail` в `true` и настройте параметры SMTP для включения подтверждений по электронной почте.

- **Логирование**

  Логирование настроено с использованием Serilog и выводится в консоль. Настройте конфигурации в `Program.cs` по мере необходимости.

- **Остановка потоков при завершении работы**

  Приложение настроено на остановку всех трансляций при завершении работы.

## Обновление приложения

1. **Получите последнюю версию кода**

   ```bash
   git pull origin main
   ```

2. **Пересоберите образы Docker**

   ```bash
   docker-compose build
   ```

3. **Перезапустите сервисы**

   ```bash
   docker-compose up -d
   ```

## Лицензия
