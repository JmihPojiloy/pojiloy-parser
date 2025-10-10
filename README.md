# JPParser - Log Analysis and Reporting System

[![.NET Core](https://img.shields.io/badge/.NET%20Core-3.1-blue.svg)](https://dotnet.microsoft.com/download/dotnet/3.1)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)]()

## 📋 Описание

**JPParser** — это система автоматизированного анализа логов веб-сервера с генерацией отчетов и отправкой результатов по электронной почте. Проект предназначен для мониторинга веб-трафика, анализа популярных страниц и отслеживания уникальных посетителей.

### 🎯 Основные возможности

- **Анализ логов IIS** с использованием Microsoft LogParser
- **Автоматическая генерация отчетов** по различным секциям сайта
- **Статистика посещений** с подсчетом уникальных IP-адресов
- **Топ-20 посетителей** по количеству обращений
- **Автоматическая отправка отчетов** по электронной почте
- **Конфигурируемые секции** для анализа различных разделов сайта

## 🏗️ Архитектура

Проект построен с использованием принципов **Clean Architecture** и разделен на следующие слои:

```
JPParser/
├── Services/           # Слой сервисов (Application)
│   ├── LogparserService.cs      # Сервис работы с LogParser
│   ├── CommandPrepareService.cs # Подготовка команд для анализа
│   └── EmailSenderService.cs   # Отправка email отчетов
├── Program.cs         # Точка входа (Presentation)
├── appsettings.json   # Конфигурация (Infrastructure)
└── parser/           # Внешние зависимости
    └── LogParser.exe # Microsoft LogParser
```

### 🔧 Принципы проектирования

- **Single Responsibility Principle** — каждый сервис отвечает за одну задачу
- **Dependency Injection** — сервисы инжектируются через конструкторы
- **Configuration-driven** — поведение настраивается через `appsettings.json`
- **Error Handling** — обработка ошибок с retry-механизмом
- **Resource Management** — автоматическая очистка временных файлов

## 🚀 Быстрый старт

### Предварительные требования

- **.NET Core 3.1** или выше
- **Microsoft LogParser** (включен в проект)
- **SMTP сервер** для отправки email
- **Лог-файлы IIS** в формате W3C

### Установка и запуск

1. **Клонирование репозитория**
   ```bash
   git clone <repository-url>
   cd pojiloy-parser
   ```

2. **Настройка конфигурации**
   
   Отредактируйте `appsettings.json`:
   ```json
   {
     "EmailSettings": {
       "FromEmail": "your-email@domain.com",
       "ToEmail": "recipient@domain.com",
       "SmtpHost": "smtp.your-provider.com",
       "SmtpPort": 587,
       "EnableSsl": true,
       "Username": "your-username",
       "Password": "your-password"
     },
     "report": {
       "source-file": "your-log-file.log",
       "top-count": 20,
       "report-sections": [
         {
           "title": "main",
           "urls": [ "/", "/Default.aspx" ]
         }
       ]
     }
   }
   ```

3. **Запуск приложения**
   ```bash
   cd src/JPParser
   dotnet run
   ```

## ⚙️ Конфигурация

### EmailSettings

| Параметр | Описание | Пример |
|----------|----------|---------|
| `FromEmail` | Email отправителя | `"bot@company.com"` |
| `ToEmail` | Email получателя | `"admin@company.com"` |
| `SmtpHost` | SMTP сервер | `"smtp.gmail.com"` |
| `SmtpPort` | Порт SMTP | `587` |
| `EnableSsl` | Использовать SSL | `true` |
| `Username` | Логин SMTP | `"bot@company.com"` |
| `Password` | Пароль SMTP | `"your-password"` |

### Report Configuration

| Параметр | Описание | Пример |
|----------|----------|---------|
| `source-file` | Имя лог-файла | `"access.log"` |
| `top-count` | Количество топ-записей | `20` |
| `report-sections` | Секции для анализа | См. пример ниже |

### Пример конфигурации секций

```json
{
  "report-sections": [
    {
      "title": "main",
      "urls": [ "/", "/Default.aspx" ]
    },
    {
      "title": "offer.search",
      "urls": [ "/Pages/Web/Offers/Default.aspx", "/offers/*" ]
    },
    {
      "title": "offer.book",
      "urls": [ "/Pages/Web/Orders/BOrder.aspx", "/book/*" ]
    }
  ]
}
```

## 📊 Формат отчетов

Система генерирует HTML-отчеты со следующей информацией:

- **Общее количество посещений** по каждой секции
- **Количество уникальных IP-адресов**
- **Топ-20 посетителей** с количеством обращений
- **Визуальное оформление** с цветовым кодированием

### Пример отчета

```html
[MAIN] >> [total-hits: 1250, unique-callers: 89] >> top 20 caller details:
192.168.1.100 - 45
192.168.1.101 - 32
...
```

## 🔧 Разработка

### Структура проекта

```
src/JPParser/
├── Services/
│   ├── LogparserService.cs      # Инкапсуляция работы с LogParser
│   ├── CommandPrepareService.cs # Генерация SQL-запросов для анализа
│   └── EmailSenderService.cs   # Отправка email с retry-логикой
├── Program.cs                   # Основная логика приложения
├── appsettings.json            # Конфигурация
└── JPParser.csproj            # Проект-файл
```

### Добавление новых секций

Для добавления новой секции анализа добавьте запись в `report-sections`:

```json
{
  "title": "new-section",
  "urls": [ "/new-page", "/another-page" ]
}
```

### Расширение функциональности

1. **Новые типы отчетов** — добавьте методы в `CommandPrepareService`
2. **Дополнительные форматы вывода** — расширьте `EmailSenderService`
3. **Новые источники данных** — создайте новые сервисы по аналогии

## 🧪 Тестирование

### Ручное тестирование

1. Поместите тестовый лог-файл в папку `parser/`
2. Обновите `source-file` в конфигурации
3. Запустите приложение и проверьте генерацию XML-файлов
4. Убедитесь в отправке email

### Отладка

```bash
# Запуск с подробным выводом
dotnet run --verbosity detailed

# Проверка конфигурации
dotnet run --configuration Debug
```

## 📦 Зависимости

- **Microsoft.Extensions.Configuration.Json** (8.0.0) — работа с конфигурацией
- **Microsoft LogParser** — анализ логов (включен в проект)
- **System.Net.Mail** — отправка email

## 🚀 Развертывание

### Docker (рекомендуется)

```dockerfile
FROM mcr.microsoft.com/dotnet/core/runtime:3.1
COPY . /app
WORKDIR /app
ENTRYPOINT ["dotnet", "JPParser.dll"]
```

### Windows Service

```bash
# Создание Windows Service
sc create "JPParser" binPath="C:\path\to\JPParser.exe"
sc start "JPParser"
```

### Linux Systemd

```ini
[Unit]
Description=JPParser Log Analysis Service
After=network.target

[Service]
Type=simple
User=jpuser
WorkingDirectory=/opt/jpparser
ExecStart=/usr/bin/dotnet /opt/jpparser/JPParser.dll
Restart=always

[Install]
WantedBy=multi-user.target
```

## 📈 Мониторинг и логирование

### Логи приложения

Приложение выводит информацию о:
- Выполнении команд LogParser
- Количестве отправленных email
- Ошибках обработки файлов
- Retry-попытках отправки

### Мониторинг производительности

- Время выполнения анализа логов
- Размер обрабатываемых файлов
- Количество сгенерированных отчетов

## 🤝 Вклад в проект

1. Fork репозитория
2. Создайте feature branch (`git checkout -b feature/amazing-feature`)
3. Commit изменения (`git commit -m 'Add amazing feature'`)
4. Push в branch (`git push origin feature/amazing-feature`)
5. Создайте Pull Request

## 📄 Лицензия

Этот проект распространяется под лицензией MIT.

## 📞 Контакты

Если у вас есть вопросы или предложения:

- **GitHub Issues**: [Создать issue](https://github.com/yourusername/pojiloy-robot/issues)
- **Email**: dmitry.podschipkov@mail.ru
- **LinkedIn**: [Профиль](www.linkedin.com/in/dpjmpj)
---

**JPParser** — надежное решение для анализа веб-логов с автоматической генерацией отчетов. 🚀
