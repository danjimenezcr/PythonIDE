from __future__ import annotations

from abc import ABC, abstractmethod
from datetime import datetime


class TerminalPrinter(ABC):
    @abstractmethod
    def format(self, text: str) -> str:
        raise NotImplementedError


class PlainTerminalPrinter(TerminalPrinter):
    def format(self, text: str) -> str:
        return text


class TimestampTerminalDecorator(TerminalPrinter):
    """Decorator: agrega marca de tiempo a la salida de terminal."""

    def __init__(self, wrapped: TerminalPrinter) -> None:
        self.wrapped = wrapped

    def format(self, text: str) -> str:
        return datetime.now().strftime("[%H:%M:%S] ") + self.wrapped.format(text)


class StatusObserver(ABC):
    @abstractmethod
    def update_status(self, message: str) -> None:
        raise NotImplementedError


class StatusSubject:
    """Observer: notifica a las vistas cuando cambia el estado."""

    def __init__(self) -> None:
        self._observers: list[StatusObserver] = []

    def attach(self, observer: StatusObserver) -> None:
        self._observers.append(observer)

    def notify(self, message: str) -> None:
        for observer in self._observers:
            observer.update_status(message)
