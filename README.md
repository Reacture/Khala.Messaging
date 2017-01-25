# Reactive Essentials - Messaging

메시지 중심 아키텍처를 지원하는 도구입니다.

## 패키지

### Core

메시징 추상화 계층을 제공합니다.

```
> Install-Package ReactiveArchitecture.Messaging.Core
```

### Azure

Microsoft Azure 메시징 서비스 대상 구현체를 제공합니다. 지원하는 서비스는 다음과 같습니다.

- Event Hubs
- Service Bus Queues
- Service Bus Topics

```
> Install-Package ReactiveArchitecture.Messaging.Azure
```

### Azure.Owin

Owin 기반 응용프로그램에 ReactiveArchitecture.Messaging.Azure 패키지를 지원하는 편의성을 제공합니다.

```
> Install-Package ReactiveArchitecture.Messaging.Azure.Owin
```

## License

```
MIT License

Copyright (c) 2017 Reactive Essentials

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
