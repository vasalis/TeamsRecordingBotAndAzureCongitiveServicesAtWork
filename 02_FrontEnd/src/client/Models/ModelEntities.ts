export class TranscriptionEntity {
    public id: string;
    public who: string;
    public text: string;
    public translations: string;
    public when: string;
}

export class CallEntity {
    callid: string;
    text: string;
    when: string;
}

export class LanguageEntity {
    key: string;
    text: string;    
}
